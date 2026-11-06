using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace Kthc {
 public class MainForm:Form {
  readonly DataGridView grid=new DataGridView{Dock=DockStyle.Fill,BackgroundColor=Color.White,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false};
  readonly TreeView nav=new TreeView{Dock=DockStyle.Left,Width=245,Font=new Font("Segoe UI",10),HideSelection=false};
  readonly FlowLayoutPanel toolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=82,Padding=new Padding(5)};
  readonly Label status=new Label{Dock=DockStyle.Bottom,Height=30,TextAlign=ContentAlignment.MiddleLeft};
  readonly DateTimePicker from=new DateTimePicker{Width=120,Format=DateTimePickerFormat.Short},to=new DateTimePicker{Width=120,Format=DateTimePickerFormat.Short};
  readonly TextBox project=new TextBox{Width=120};
  DataTable table; string current="Vouchers"; bool editing;
  readonly Dictionary<string,string> masters=new Dictionary<string,string>{
   {"Settings","Thông tin đơn vị"},{"Accounts","Tài khoản (danh mục thử nghiệm)"},{"Parties","Đối tượng / nhà thầu"},{"Funds","Nguồn kinh phí"},{"BudgetItems","Mục lục ngân sách"},{"Projects","Dự án / công trình"},{"Contracts","Hợp đồng xây dựng"},{"CapitalPlans","Kế hoạch vốn"},{"Acceptances","Biên bản nghiệm thu"},{"Assets","Sổ tài sản"},{"Items","Danh mục vật tư"},{"StockMoves","Nhập / xuất kho chi tiết"},{"Employees","Nhân viên"},{"Payroll","Bảng lương nhập liệu"}};
  public MainForm() {
   Text="KTHC • Kế toán HCSN và XDCB • Bản phát triển 0.1"; Width=1400; Height=850; MinimumSize=new Size(1050,650); Font=new Font("Segoe UI",9); StartPosition=FormStartPosition.CenterScreen;
   from.Value=new DateTime(DateTime.Today.Year,1,1); to.Value=DateTime.Today;
   var banner=new Label{Text="KTHC  |  OFFLINE      Bản phát triển — chưa nghiệm thu biểu mẫu pháp định",Dock=DockStyle.Top,Height=55,BackColor=Color.FromArgb(20,45,72),ForeColor=Color.White,Font=new Font("Segoe UI",14,FontStyle.Bold),Padding=new Padding(14,14,0,0)};
   var dm=nav.Nodes.Add("Danh mục & nghiệp vụ chi tiết"); foreach(var kv in masters) { var n=dm.Nodes.Add(kv.Value); n.Tag=kv.Key; }
   var journal=nav.Nodes.Add("Chứng từ & sổ kế toán"); Add(journal,"Chứng từ kế toán","Vouchers"); Add(journal,"Sổ nhật ký / sổ cái","Ledger"); Add(journal,"Cân đối phát sinh (quản trị)","Trial"); Add(journal,"Công nợ theo đối tượng","Debt"); Add(journal,"Thu / chi theo nguồn, MLNS","Budget");
   var xd=nav.Nodes.Add("Báo cáo xây dựng cơ bản"); Add(xd,"Tổng hợp dự án","ProjectReport"); Add(xd,"Đối chiếu hợp đồng","ContractReport"); Add(xd,"Chi phí 241 theo khoản mục","Construction");
   var other=nav.Nodes.Add("Quản trị & đối chiếu"); Add(other,"Tồn kho số lượng","Stock"); Add(other,"Lương thực nhận","Salary"); Add(other,"Kỳ kế toán","Periods"); Add(other,"Nhật ký thao tác","Audit");
   nav.AfterSelect+=(s,e)=>{ if(e.Node.Tag==null)return; if(editing && MessageBox.Show("Bỏ các thay đổi chưa lưu?","Chuyển màn hình",MessageBoxButtons.YesNo)!=DialogResult.Yes)return; current=(string)e.Node.Tag; LoadCurrent(); };
   Controls.Add(grid); Controls.Add(toolbar); Controls.Add(status); Controls.Add(nav); Controls.Add(banner); dm.Expand(); journal.Expand(); xd.Expand(); LoadCurrent();
  }
  static void Add(TreeNode n,string label,string tag) { n.Nodes.Add(new TreeNode(label){Tag=tag}); }
  void Button(string text,Action act) { var b=new Button{Text=text,AutoSize=true,Height=29}; b.Click+=(s,e)=>Run(act); toolbar.Controls.Add(b); }
  void Run(Action a) { try { a(); } catch(Exception ex) { MessageBox.Show(ex.Message,"KTHC",MessageBoxButtons.OK,MessageBoxIcon.Warning); } }
  SqlParameter[] Dates() { return new[]{new SqlParameter("@from",from.Value.Date),new SqlParameter("@to",to.Value.Date),new SqlParameter("@project",project.Text.Trim())}; }
  string Scope { get {return "VoucherDate BETWEEN @from AND @to AND (@project='' OR ProjectCode=@project)";} }
  void LoadCurrent() { Run(()=>{
   editing=false; toolbar.Controls.Clear(); grid.ReadOnly=true; grid.AllowUserToAddRows=false; grid.AllowUserToDeleteRows=false;
   Button("Tải lại",LoadCurrent); Button("Xuất CSV",Export); Button("Sao lưu database",Backup);
   if(masters.ContainsKey(current)) {
    table=Database.Query("SELECT * FROM ["+current+"]"); Button("Sửa / thêm",()=>{editing=true;grid.ReadOnly=false;grid.AllowUserToAddRows=true;grid.AllowUserToDeleteRows=true; foreach(DataColumn col in table.Columns) if(col.AutoIncrement||col.ColumnName=="Id") grid.Columns[col.ColumnName].ReadOnly=true;status.Text="Đang sửa. Ô mã tham chiếu phải tồn tại trong danh mục. Ngày: định dạng Windows.";}); Button("Lưu thay đổi",SaveMaster);
   } else if(current=="Vouchers") {
    table=Database.Query("SELECT Id,Number,VoucherDate,Kind,Description,Status,ReversesId,PostedAt FROM Vouchers ORDER BY VoucherDate DESC,Id DESC");
    Button("Thêm chứng từ",()=>{using(var f=new VoucherForm())f.ShowDialog(this);LoadCurrent();}); Button("Mở chứng từ",()=>{using(var f=new VoucherForm(SelectedId()))f.ShowDialog(this);LoadCurrent();});
    Button("Ghi sổ",()=>{Accounting.Post(SelectedId());LoadCurrent();}); Button("Đảo chứng từ",Reverse);
   } else if(current=="Periods") { table=Database.Query("SELECT * FROM Periods ORDER BY Period DESC"); Button("Khóa kỳ",()=>Period(true)); Button("Mở khóa kỳ",()=>Period(false)); }
   else if(current=="Audit") table=Database.Query("SELECT TOP 1000 * FROM Audit ORDER BY Id DESC");
   else if(current=="Stock") table=Database.Query("SELECT ItemCode,Warehouse,SUM(Quantity) Quantity FROM StockMoves GROUP BY ItemCode,Warehouse");
   else if(current=="Salary") table=Database.Query("SELECT p.Period,e.Code,e.Name,p.Salary,p.Allowance,p.Deduction,p.Salary+p.Allowance-p.Deduction Net FROM Payroll p JOIN Employees e ON e.Code=p.EmployeeCode ORDER BY p.Period,e.Code");
   else if(current=="ProjectReport") table=Database.Query("SELECT * FROM ProjectSummary");
   else if(current=="ContractReport") table=Database.Query("SELECT * FROM ContractSummary");
   else {
    toolbar.Controls.AddRange(new Control[]{new Label{Text="Từ / đến",AutoSize=true},from,to,new Label{Text="Mã dự án",AutoSize=true},project}); Button("Lọc",LoadCurrent);
    string sql;
    if(current=="Ledger") sql="SELECT * FROM Ledger WHERE "+Scope+" ORDER BY VoucherDate,VoucherId";
    else if(current=="Trial") sql="WITH s AS (SELECT AccountCode,AccountName,OffBalance,SUM(CASE WHEN VoucherDate<@from THEN Debit-Credit ELSE 0 END) OpeningNet,SUM(CASE WHEN VoucherDate>=@from THEN Debit ELSE 0 END) Debit,SUM(CASE WHEN VoucherDate>=@from THEN Credit ELSE 0 END) Credit FROM Ledger WHERE VoucherDate<=@to AND (@project='' OR ProjectCode=@project) GROUP BY AccountCode,AccountName,OffBalance) SELECT *,OpeningNet+Debit-Credit ClosingNet FROM s ORDER BY AccountCode";
    else if(current=="Debt") sql="SELECT AccountCode,PartyCode,ContractCode,SUM(Debit) Debit,SUM(Credit) Credit,SUM(Debit-Credit) Net FROM Ledger WHERE "+Scope+" AND (AccountCode LIKE '131%' OR AccountCode LIKE '331%' OR AccountCode LIKE '141%') GROUP BY AccountCode,PartyCode,ContractCode";
    else if(current=="Construction") sql="SELECT ProjectCode,ContractCode,CostCategory,SUM(Debit) Debit,SUM(Credit) Credit,SUM(Debit-Credit) Net FROM Ledger WHERE "+Scope+" AND AccountCode LIKE '241%' GROUP BY ProjectCode,ContractCode,CostCategory";
    else sql="SELECT FundCode,BudgetCode,AccountCode,OffBalance,SUM(Debit) Debit,SUM(Credit) Credit FROM Ledger WHERE "+Scope+" GROUP BY FundCode,BudgetCode,AccountCode,OffBalance";
    if(from.Value.Date>to.Value.Date) throw new ArgumentException("Ngày bắt đầu phải trước ngày kết thúc."); table=Database.Query(sql,Dates());
   }
   grid.DataSource=table; foreach(DataGridViewColumn c in grid.Columns) { if(c.ValueType==typeof(decimal)) c.DefaultCellStyle.Format="N2"; if(c.ValueType==typeof(DateTime)) c.DefaultCellStyle.Format="dd/MM/yyyy"; }
   status.Text=table.Rows.Count+" dòng | "+Environment.UserName+" | Báo cáo quản trị; bảng chi tiết kho, lương, nghiệm thu chưa tự ghi sổ. Tổng hợp dự án/hợp đồng: lũy kế toàn bộ.";
  }); }
  int SelectedId() { if(grid.CurrentRow==null) throw new Exception("Chọn một chứng từ."); return Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value); }
  void Reverse() { int id=SelectedId(); string n=Prompt("Số chứng từ đảo (duy nhất)","DAO-"+id+"-"+DateTime.Now.ToString("yyyyMMddHHmmss")); if(n==null)return; string d=Prompt("Ngày chứng từ đảo: yyyy-MM-dd",DateTime.Today.ToString("yyyy-MM-dd")); if(d==null)return; DateTime date; if(!DateTime.TryParseExact(d,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out date))throw new Exception("Ngày không hợp lệ."); Accounting.Reverse(id,date,n);LoadCurrent(); }
  void Period(bool locked) { string p=Prompt("Kỳ kế toán yyyy-MM",DateTime.Today.ToString("yyyy-MM")); if(p==null)return; Accounting.SetPeriod(p,locked);LoadCurrent(); }
  public static string Prompt(string title,string value) { using(var f=new Form{Text=title,Width=520,Height=155,StartPosition=FormStartPosition.CenterParent}) { var x=new TextBox{Text=value,Dock=DockStyle.Top}; var ok=new Button{Text="Đồng ý",Dock=DockStyle.Bottom,DialogResult=DialogResult.OK}; f.Controls.Add(x);f.Controls.Add(ok);f.AcceptButton=ok;return f.ShowDialog()==DialogResult.OK?x.Text:null; } }
  void SaveMaster() {
   if(!editing)return; grid.EndEdit(); BindingContext[table].EndCurrentEdit();
   using(var c=Database.Open()) using(var tx=c.BeginTransaction(IsolationLevel.Serializable)) {
    Accounting.Gate(c,tx);
    using(var da=new SqlDataAdapter("SELECT * FROM ["+current+"]",c)) using(var builder=new SqlCommandBuilder(da)) {
     da.SelectCommand.Transaction=tx; da.InsertCommand=builder.GetInsertCommand(); da.UpdateCommand=builder.GetUpdateCommand(); da.DeleteCommand=builder.GetDeleteCommand();
     da.InsertCommand.Transaction=da.UpdateCommand.Transaction=da.DeleteCommand.Transaction=tx;
     da.AcceptChangesDuringUpdate=false; da.Update(table);
    }
    ValidateMaster(c,tx); Database.Audit(c,tx,"EDIT",current,"Rows changed: "+table.Rows.Cast<DataRow>().Count(r=>r.RowState!=DataRowState.Unchanged)); tx.Commit();
   }
   LoadCurrent();
  }
  void ValidateMaster(SqlConnection c,SqlTransaction t) {
   string sql="";
   if(current=="Acceptances"||current=="Contracts") sql="IF EXISTS(SELECT c.Code FROM Contracts c JOIN Acceptances a ON a.ContractCode=c.Code GROUP BY c.Code,c.Value HAVING SUM(a.Amount)>c.Value) THROW 51201,N'Nghiệm thu lũy kế vượt giá trị hợp đồng.',1; IF EXISTS(SELECT 1 FROM Contracts c JOIN Lines l ON l.ContractCode=c.Code JOIN Vouchers v ON v.Id=l.VoucherId WHERE v.Status='Posted' AND (l.ProjectCode<>c.ProjectCode OR l.PartyCode<>c.PartyCode)) THROW 51202,N'Hợp đồng đã phát sinh: không đổi nhà thầu/dự án.',1;";
   if(current=="CapitalPlans"||current=="Projects") sql="IF EXISTS(SELECT p.Code FROM Projects p JOIN CapitalPlans a ON a.ProjectCode=p.Code GROUP BY p.Code,p.ApprovedBudget HAVING SUM(a.Amount)>p.ApprovedBudget) THROW 51203,N'Tổng kế hoạch vốn vượt tổng mức đầu tư.',1;";
   if(current=="StockMoves") sql="IF EXISTS(SELECT 1 FROM (SELECT SUM(Quantity) OVER(PARTITION BY ItemCode,Warehouse ORDER BY MoveDate,Id ROWS UNBOUNDED PRECEDING) Balance FROM StockMoves) s WHERE Balance<0) THROW 51204,N'Xuất kho gây âm tồn tại một thời điểm.',1;";
   if(current=="Payroll"||current=="StockMoves"||current=="Acceptances") {
    string col=current=="Payroll"?"Period":current=="StockMoves"?"MoveDate":"AcceptanceDate";
    foreach(DataRow row in table.Rows) if(row.RowState!=DataRowState.Unchanged) foreach(DataRowVersion ver in new[]{DataRowVersion.Original,DataRowVersion.Current}) if(row.HasVersion(ver)) {
     object raw=row[col,ver]; string period=current=="Payroll"?Convert.ToString(raw):Convert.ToDateTime(raw).ToString("yyyy-MM"); DateTime valid; if(!DateTime.TryParseExact(period+"-01","yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out valid)) throw new Exception("Kỳ không hợp lệ.");
     using(var q=new SqlCommand("SELECT COUNT(*) FROM Periods WHERE Period=@p AND Locked=1",c,t)) {q.Parameters.AddWithValue("@p",period);if((int)q.ExecuteScalar()>0) throw new Exception("Kỳ đã khóa: "+period);}
    }
   }
   if(sql!="") using(var q=new SqlCommand(sql,c,t)) q.ExecuteNonQuery();
  }
  void Backup() { using(var d=new SaveFileDialog{Filter="SQL Server backup|*.bak",FileName="Kthc_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".bak",InitialDirectory=Database.Folder}) if(d.ShowDialog()==DialogResult.OK) { if(File.Exists(d.FileName))throw new Exception("Chọn tên mới, không ghi đè bản sao lưu.");Database.Backup(d.FileName);MessageBox.Show("Đã sao lưu database. Xem hướng dẫn khôi phục trong docs/INSTALL.md."); } }
  static string Csv(object v) {string s=v==DBNull.Value?"":Convert.ToString(v,System.Globalization.CultureInfo.InvariantCulture);if(s.Length>0&&"=+-@\t\r".IndexOf(s[0])>=0&&!(v is decimal)&&!(v is int))s="'"+s;return "\""+s.Replace("\"","\"\"")+"\"";}
  void Export() { using(var d=new SaveFileDialog{Filter="CSV UTF-8|*.csv",FileName=current+".csv"}) if(d.ShowDialog()==DialogResult.OK) using(var w=new StreamWriter(d.FileName,false,new UTF8Encoding(true))) {w.WriteLine(string.Join(",",table.Columns.Cast<DataColumn>().Select(x=>Csv(x.ColumnName))));foreach(DataRow r in table.Rows)if(r.RowState!=DataRowState.Deleted)w.WriteLine(string.Join(",",r.ItemArray.Select(Csv)));} }
 }
}
