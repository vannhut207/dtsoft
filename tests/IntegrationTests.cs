using System;
using System.Data;
using System.Data.SqlClient;
using Kthc;
class IntegrationTests {
 static int passed;
 static void Assert(bool condition,string name) { if(!condition)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name); }
 static void Fails(Action action,string name) { bool failed=false;try{action();}catch{failed=true;}Assert(failed,name); }
 static void Exec(string sql) {using(var c=Database.Open())using(var q=new SqlCommand(sql,c))q.ExecuteNonQuery();}
 static DataTable Lines(decimal debit,decimal credit) {var t=Accounting.NewLines();t.Rows.Add("111",DBNull.Value,DBNull.Value,DBNull.Value,DBNull.Value,DBNull.Value,DBNull.Value,debit,0m);t.Rows.Add("112",DBNull.Value,DBNull.Value,DBNull.Value,DBNull.Value,DBNull.Value,DBNull.Value,0m,credit);return t;}
 static int Main() {
  string name="KthcTest_"+Guid.NewGuid().ToString("N");
  try {
   Database.Initialize(name);Database.Initialize(name);var d=new DateTime(2026,1,10);
   Assert(Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM Settings"))==1,"Migration is idempotent");
   int id=Accounting.SaveDraft(0,"T001",d,"Tổng hợp","Nộp tiền",Lines(100,100));
   Assert(Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM Ledger"))==0,"Draft excluded from ledger");
   Accounting.Post(id);Assert(Convert.ToDecimal(Database.Scalar("SELECT SUM(Debit-Credit) FROM Ledger WHERE OffBalance=0"))==0,"Balanced posting");
   Fails(()=>Accounting.Post(id),"Double posting rejected");
   Fails(()=>Accounting.SaveDraft(id,"T001",d,"Tổng hợp","Edit",Lines(200,200)),"Posted edit rejected");
   Fails(()=>Exec("UPDATE Lines SET Debit=200 WHERE VoucherId="+id+" AND Debit>0"),"Database blocks posted line edit");
   Fails(()=>Exec("DELETE Vouchers WHERE Id="+id),"Database blocks posted delete");
   Fails(()=>Exec("UPDATE Accounts SET OffBalance=1 WHERE Code='111'"),"Account classification frozen after posting");
   int bad=Accounting.SaveDraft(0,"BAD",d,"Tổng hợp","Lệch",Lines(100,90));Fails(()=>Accounting.Post(bad),"Unbalanced posting rejected");
   Assert(Convert.ToString(Database.Scalar("SELECT Status FROM Vouchers WHERE Id="+bad))=="Draft","Failed posting rolls back");
   Fails(()=>Accounting.SaveDraft(0,"T001",d,"Tổng hợp","Trùng",Lines(1,1)),"Duplicate voucher number rejected");
   Fails(()=>Accounting.SaveDraft(0,"BADDEC",d,"Tổng hợp","Precision",Lines(1.001m,1.001m)),"Fraction precision enforced");
   var ob=Accounting.NewLines();ob.Rows.Add("008",DBNull.Value,DBNull.Value,DBNull.Value,DBNull.Value,DBNull.Value,DBNull.Value,100m,0m);int outside=Accounting.SaveDraft(0,"OB",d,"Tổng hợp","Ngoài bảng",ob);Accounting.Post(outside);
   Assert(Convert.ToDecimal(Database.Scalar("SELECT SUM(Debit) FROM Ledger WHERE OffBalance=1"))==100,"Off-balance single entry supported");
   Accounting.SetPeriod("2026-01",true);Fails(()=>Accounting.SaveDraft(0,"LOCK",d,"Tổng hợp","Khóa",Lines(1,1)),"Locked period blocks draft");
   Fails(()=>Accounting.SaveDraft(bad,"BAD",d.AddMonths(1),"Tổng hợp","Move",Lines(1,1)),"Locked draft cannot move to open period");
   Fails(()=>Accounting.Reverse(id,d,"REVLOCK"),"Locked period blocks reversal");
   int rev=Accounting.Reverse(id,d.AddMonths(1),"REV");Assert(rev>id,"Reversal created in open period");
   Assert(Convert.ToDecimal(Database.Scalar("SELECT SUM(Debit-Credit) FROM Ledger WHERE AccountCode='111'"))==0,"Reversal cancels account balance");
   Fails(()=>Accounting.Reverse(id,d.AddMonths(1),"REV2"),"Second reversal rejected");
   Assert(Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM Vouchers WHERE Number='REV2'"))==0,"Failed reversal atomic");
   Exec("INSERT Parties(Code,Name) VALUES('P',N'Nhà thầu'); INSERT Projects(Code,Name,ApprovedBudget) VALUES('DA',N'Dự án',1000),('OTHER',N'Khác',1000); INSERT Contracts(Code,ProjectCode,PartyCode,Name,SignedDate,Value) VALUES('HD','DA','P',N'Hợp đồng','2026-02-01',900);");
   var construction=Accounting.NewLines();construction.Rows.Add("241","P",DBNull.Value,DBNull.Value,"DA","HD","Xây lắp",500m,0m);construction.Rows.Add("331","P",DBNull.Value,DBNull.Value,"DA","HD","Xây lắp",0m,500m);
   int xd=Accounting.SaveDraft(0,"XD",d.AddMonths(1),"XDCB","Chi phí công trình",construction);Accounting.Post(xd);
   Assert(Convert.ToDecimal(Database.Scalar("SELECT ConstructionBalance FROM ProjectSummary WHERE Code='DA'"))==500,"Project cost aggregates posted 241");
   construction.Rows[0]["ProjectCode"]="OTHER";int mismatch=Accounting.SaveDraft(0,"MISMATCH",d.AddMonths(1),"XDCB","Sai dự án",construction);Fails(()=>Accounting.Post(mismatch),"Contract project mismatch rejected");
   Assert(Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM Audit"))>0,"Audit recorded");
   Console.WriteLine("TOTAL PASS: "+passed);return 0;
  } catch(Exception e) {Console.Error.WriteLine(e);return 1;}
  finally {try{SqlConnection.ClearAllPools();using(var c=new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Database=master")){c.Open();using(var q=new SqlCommand("ALTER DATABASE ["+name+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+name+"]",c))q.ExecuteNonQuery();}}catch(Exception e){Console.WriteLine("Cleanup: "+e.Message);}}
 }
}
