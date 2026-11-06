using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
namespace Kthc {
 public static class Database {
  public static string ConnectionString { get; private set; }
  public static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Kthc");
  public static void Initialize(string databaseName="KthcOffline") {
   if(!Regex.IsMatch(databaseName,"^[A-Za-z][A-Za-z0-9_]{0,63}$")) throw new ArgumentException("Tên database không hợp lệ");
   Directory.CreateDirectory(Folder);
   var b=new SqlConnectionStringBuilder { DataSource=@"(LocalDB)\MSSQLLocalDB", IntegratedSecurity=true, InitialCatalog="master", ConnectTimeout=30 };
   using(var c=new SqlConnection(b.ConnectionString)) { c.Open(); using(var cmd=c.CreateCommand()) { cmd.CommandText="IF DB_ID(@name) IS NULL BEGIN CREATE DATABASE ["+databaseName+"] END"; cmd.Parameters.AddWithValue("@name",databaseName); cmd.ExecuteNonQuery(); } }
   b.InitialCatalog=databaseName; ConnectionString=b.ConnectionString;
   // One atomic migration: failed initial creation can be retried safely.
   using(var c=Open()) using(var t=c.BeginTransaction()) {
    using(var cmd=new SqlCommand("EXEC sp_getapplock @Resource='KthcSchema',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000",c,t)) cmd.ExecuteNonQuery();
    using(var cmd=new SqlCommand("SELECT OBJECT_ID('dbo.Settings')",c,t)) {
     if(cmd.ExecuteScalar()==DBNull.Value) {
      string sql; using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("Kthc.schema.sql")) using(var r=new StreamReader(s)) sql=r.ReadToEnd();
      foreach(string batch in Regex.Split(sql,@"^GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase)) if(!string.IsNullOrWhiteSpace(batch)) using(var q=new SqlCommand(batch,c,t)) q.ExecuteNonQuery();
     }
    }
    t.Commit();
   }
  }
  public static SqlConnection Open() { var c=new SqlConnection(ConnectionString); c.Open(); return c; }
  public static DataTable Query(string sql,params SqlParameter[] ps) { using(var c=Open()) using(var cmd=new SqlCommand(sql,c)) { cmd.Parameters.AddRange(ps); using(var da=new SqlDataAdapter(cmd)) { var table=new DataTable(); da.Fill(table); return table; } } }
  public static object Scalar(string sql,params SqlParameter[] ps) { using(var c=Open()) using(var cmd=new SqlCommand(sql,c)) { cmd.Parameters.AddRange(ps); return cmd.ExecuteScalar(); } }
  public static void Audit(SqlConnection c,SqlTransaction t,string action,string entity,string detail) { using(var cmd=new SqlCommand("INSERT Audit(Actor,Action,Entity,Detail) VALUES(@a,@b,@c,@d)",c,t)) { cmd.Parameters.AddWithValue("@a",Environment.UserName); cmd.Parameters.AddWithValue("@b",action); cmd.Parameters.AddWithValue("@c",entity); cmd.Parameters.AddWithValue("@d",detail); cmd.ExecuteNonQuery(); } }
  public static void Backup(string path) {
   // SQL native backup includes transactionally consistent data; never copy live MDF.
   using(var c=Open()) using(var cmd=c.CreateCommand()) { cmd.CommandTimeout=120; cmd.CommandText="BACKUP DATABASE ["+c.Database.Replace("]","]]")+"] TO DISK=@p WITH COPY_ONLY, CHECKSUM"; cmd.Parameters.AddWithValue("@p",path); cmd.ExecuteNonQuery(); }
  }
 }
}
