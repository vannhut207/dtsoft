using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
namespace Kthc {
 public class VoucherForm:Form {
  int id; TextBox number=new TextBox{Width=150},description=new TextBox{Width=400}; DateTimePicker date=new DateTimePicker{Width=125,Format=DateTimePickerFormat.Short}; ComboBox kind=new ComboBox{Width=150,DropDownStyle=ComboBoxStyle.DropDownList}; DataGridView grid=new DataGridView{Dock=DockStyle.Fill,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells,BackgroundColor=Color.White}; DataTable lines;
  public VoucherForm(int voucherId=0) {
   id=voucherId; Text="Chứng từ kế toán — lưu nháp rồi ghi sổ"; Width=1220; Height=600; StartPosition=FormStartPosition.CenterParent;
   kind.Items.AddRange(new object[]{"Phiếu thu","Phiếu chi","Ngân hàng/Kho bạc","Mua hàng","XDCB","Tổng hợp","Số dư đầu kỳ"}); kind.SelectedIndex=5;
   var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=82,Padding=new Padding(8)};
   top.Controls.AddRange(new Control[]{new Label{Text="Số CT",AutoSize=true},number,new Label{Text="Ngày",AutoSize=true},date,kind,new Label{Text="Diễn giải",AutoSize=true},description});
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=50}; var save=new Button{Text="Lưu nháp",Width=120}; save.Click+=(s,e)=>Save(false); var post=new Button{Text="Lưu và ghi sổ",Width=150}; post.Click+=(s,e)=>Save(true); bottom.Controls.AddRange(new Control[]{save,post,new Label{Text="Nợ/Có: một dòng mỗi tài khoản. Ngoài bảng ghi đơn. Mã phải có trong danh mục.",AutoSize=true,Padding=new Padding(5,10,0,0)}});
   lines=Accounting.NewLines();
   if(id>0) { var h=Database.Query("SELECT * FROM Vouchers WHERE Id=@id",new SqlParameter("@id",id)); if(h.Rows.Count!=1) throw new Exception("Không tìm thấy chứng từ."); var r=h.Rows[0]; number.Text=r["Number"].ToString(); date.Value=(DateTime)r["VoucherDate"]; description.Text=r["Description"].ToString(); kind.SelectedItem=r["Kind"].ToString(); if(kind.SelectedIndex<0) kind.SelectedIndex=5; lines=Database.Query("SELECT AccountCode,PartyCode,FundCode,BudgetCode,ProjectCode,ContractCode,CostCategory,Debit,Credit FROM Lines WHERE VoucherId=@id ORDER BY Id",new SqlParameter("@id",id)); if(r["Status"].ToString()=="Posted") { grid.ReadOnly=true; grid.AllowUserToAddRows=false; grid.AllowUserToDeleteRows=false; save.Enabled=post.Enabled=false; top.Enabled=false; } }
   grid.DataSource=lines; string[] labels={"Tài khoản","Đối tượng","Nguồn","Mục lục NS","Dự án","Hợp đồng","Khoản mục chi phí","Nợ","Có"}; for(int i=0;i<labels.Length;i++) grid.Columns[i].HeaderText=labels[i]; grid.Columns["Debit"].DefaultCellStyle.Format="N2"; grid.Columns["Credit"].DefaultCellStyle.Format="N2";
   Controls.Add(grid); Controls.Add(top); Controls.Add(bottom);
  }
  void Save(bool post) { try { grid.EndEdit(); BindingContext[lines].EndCurrentEdit(); id=Accounting.SaveDraft(id,number.Text,date.Value,kind.Text,description.Text,lines); if(post) Accounting.Post(id); DialogResult=DialogResult.OK; Close(); } catch(Exception ex) { MessageBox.Show(ex.Message,"Chưa hoàn tất",MessageBoxButtons.OK,MessageBoxIcon.Warning); } }
 }
}
