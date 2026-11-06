using System;
using System.Data;
using System.Data.SqlClient;
namespace Kthc {
 public static class Accounting {
  public static void Gate(SqlConnection c,SqlTransaction t) { using(var cmd=new SqlCommand("DECLARE @r int; EXEC @r=sp_getapplock @Resource='KthcWrite',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; IF @r<0 THROW 51100,N'Dữ liệu đang được xử lý, hãy thử lại.',1;",c,t)) cmd.ExecuteNonQuery(); }
  static void CheckPeriod(SqlConnection c,SqlTransaction t,DateTime date) { using(var cmd=new SqlCommand("SELECT COUNT(*) FROM Periods WHERE Period=@p AND Locked=1",c,t)) { cmd.Parameters.AddWithValue("@p",date.ToString("yyyy-MM")); if((int)cmd.ExecuteScalar()>0) throw new InvalidOperationException("Kỳ đã khóa."); } }
  public static DataTable NewLines() { var t=new DataTable(); foreach(var n in new[]{"AccountCode","PartyCode","FundCode","BudgetCode","ProjectCode","ContractCode","CostCategory"}) t.Columns.Add(n,typeof(string)); t.Columns.Add("Debit",typeof(decimal)); t.Columns.Add("Credit",typeof(decimal)); return t; }
  public static int SaveDraft(int id,string number,DateTime date,string kind,string description,DataTable lines,int? reverses=null) {
   if(string.IsNullOrWhiteSpace(number)||string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Nhập số chứng từ và diễn giải.");
   if(lines.Rows.Count==0) throw new ArgumentException("Nhập ít nhất một dòng định khoản.");
   using(var c=Database.Open()) using(var tx=c.BeginTransaction(IsolationLevel.Serializable)) {
    Gate(c,tx); CheckPeriod(c,tx,date);
    if(id>0) using(var q=new SqlCommand("SELECT VoucherDate FROM Vouchers WHERE Id=@id AND Status='Draft'",c,tx)) { q.Parameters.AddWithValue("@id",id); var old=q.ExecuteScalar(); if(old==null) throw new InvalidOperationException("Chứng từ không còn ở trạng thái nháp."); CheckPeriod(c,tx,(DateTime)old); }
    using(var q=new SqlCommand(id==0?"INSERT Vouchers(Number,VoucherDate,Kind,Description,ReversesId) OUTPUT INSERTED.Id VALUES(@n,@d,@k,@x,@r)":"UPDATE Vouchers SET Number=@n,VoucherDate=@d,Kind=@k,Description=@x WHERE Id=@id AND Status='Draft'; SELECT @id",c,tx)) {
     q.Parameters.AddWithValue("@n",number.Trim()); q.Parameters.AddWithValue("@d",date.Date); q.Parameters.AddWithValue("@k",kind); q.Parameters.AddWithValue("@x",description); q.Parameters.AddWithValue("@r",(object)reverses??DBNull.Value); q.Parameters.AddWithValue("@id",id); id=Convert.ToInt32(q.ExecuteScalar());
    }
    using(var q=new SqlCommand("DELETE Lines WHERE VoucherId=@id",c,tx)) { q.Parameters.AddWithValue("@id",id); q.ExecuteNonQuery(); }
    foreach(DataRow r in lines.Rows) {
     if(r.RowState==DataRowState.Deleted) continue;
     decimal debit=r.IsNull("Debit")?0:Convert.ToDecimal(r["Debit"]),credit=r.IsNull("Credit")?0:Convert.ToDecimal(r["Credit"]);
     if(debit<0||credit<0||(debit==0)==(credit==0)||decimal.Round(debit,2)!=debit||decimal.Round(credit,2)!=credit) throw new ArgumentException("Mỗi dòng chỉ ghi Nợ hoặc Có dương, tối đa 2 số lẻ.");
     using(var q=new SqlCommand("INSERT Lines(VoucherId,AccountCode,Debit,Credit,PartyCode,FundCode,BudgetCode,ProjectCode,ContractCode,CostCategory) VALUES(@id,@AccountCode,@Debit,@Credit,@PartyCode,@FundCode,@BudgetCode,@ProjectCode,@ContractCode,@CostCategory)",c,tx)) {
      q.Parameters.AddWithValue("@id",id); foreach(DataColumn col in lines.Columns) { object v=r[col]; if(v is string && string.IsNullOrWhiteSpace((string)v)) v=DBNull.Value; q.Parameters.AddWithValue("@"+col.ColumnName,v); } q.Parameters["@Debit"].Value=debit; q.Parameters["@Credit"].Value=credit; q.ExecuteNonQuery();
     }
    }
    Database.Audit(c,tx,"SAVE_DRAFT","Voucher:"+id,number); tx.Commit(); return id;
   }
  }
  public static void Post(int id) {
   using(var c=Database.Open()) using(var t=c.BeginTransaction(IsolationLevel.Serializable)) {
    Gate(c,t);
    using(var cmd=new SqlCommand("UPDATE Vouchers SET Status='Posted',PostedAt=SYSUTCDATETIME() WHERE Id=@id AND Status='Draft'",c,t)) { cmd.Parameters.AddWithValue("@id",id); if(cmd.ExecuteNonQuery()!=1) throw new InvalidOperationException("Chỉ được ghi sổ chứng từ nháp."); }
    Database.Audit(c,t,"POST","Voucher:"+id,""); t.Commit();
   }
  }
  public static int Reverse(int id,DateTime date,string number) {
   // Reverse inside a single transaction, no partially created reversal.
   using(var c=Database.Open()) using(var t=c.BeginTransaction(IsolationLevel.Serializable)) {
    Gate(c,t); CheckPeriod(c,t,date);
    int newId; using(var q=new SqlCommand("INSERT Vouchers(Number,VoucherDate,Kind,Description,ReversesId) OUTPUT INSERTED.Id SELECT @n,@d,N'Đảo chứng từ',N'Đảo '+Number+N': '+Description,Id FROM Vouchers WHERE Id=@id AND Status='Posted'",c,t)) { q.Parameters.AddWithValue("@n",number); q.Parameters.AddWithValue("@d",date.Date); q.Parameters.AddWithValue("@id",id); var v=q.ExecuteScalar(); if(v==null) throw new InvalidOperationException("Chỉ đảo chứng từ đã ghi sổ."); newId=Convert.ToInt32(v); }
    using(var q=new SqlCommand("INSERT Lines(VoucherId,AccountCode,Debit,Credit,PartyCode,FundCode,BudgetCode,ProjectCode,ContractCode,CostCategory) SELECT @new,AccountCode,Credit,Debit,PartyCode,FundCode,BudgetCode,ProjectCode,ContractCode,CostCategory FROM Lines WHERE VoucherId=@old",c,t)) { q.Parameters.AddWithValue("@new",newId); q.Parameters.AddWithValue("@old",id); q.ExecuteNonQuery(); }
    using(var q=new SqlCommand("UPDATE Vouchers SET Status='Posted',PostedAt=SYSUTCDATETIME() WHERE Id=@id",c,t)) { q.Parameters.AddWithValue("@id",newId); q.ExecuteNonQuery(); }
    Database.Audit(c,t,"REVERSE","Voucher:"+id,"Reversal:"+newId); t.Commit(); return newId;
   }
  }
  public static void SetPeriod(string period,bool locked) {
   DateTime parsed; if(!DateTime.TryParseExact(period+"-01","yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out parsed)) throw new ArgumentException("Kỳ phải có dạng yyyy-MM.");
   using(var c=Database.Open()) using(var t=c.BeginTransaction(IsolationLevel.Serializable)) { Gate(c,t); using(var q=new SqlCommand("IF EXISTS(SELECT 1 FROM Periods WHERE Period=@p) UPDATE Periods SET Locked=@l WHERE Period=@p ELSE INSERT Periods VALUES(@p,@l)",c,t)) { q.Parameters.AddWithValue("@p",period); q.Parameters.AddWithValue("@l",locked); q.ExecuteNonQuery(); } Database.Audit(c,t,locked?"LOCK":"UNLOCK","Period",period); t.Commit(); }
  }
 }
}
