using System;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

namespace demo
{
    public partial class OrderEditWindow : Window
    {
        readonly int? _id;

        public OrderEditWindow(int? id)
        {
            InitializeComponent();
            _id = id;
            Title = id == null ? "Добавление заказа" : "Редактирование";
            IdLabel.Visibility = IdBox.Visibility = id == null ? Visibility.Collapsed : Visibility.Visible;
            OrderDate.SelectedDate = DateTime.Today;
            DeliveryDate.SelectedDate = DateTime.Today.AddDays(6);
            ArticuleCombo.ItemsSource = Db.Lookup("SELECT 0, articule FROM Products ORDER BY articule");
            StatusCombo.ItemsSource = Db.Lookup("SELECT id_status, status_name FROM Orders_status");
            AddressCombo.ItemsSource = Db.Lookup("SELECT id_adress, adress FROM Delivery_adress ORDER BY id_adress");
            ArticuleCombo.SelectedIndex = StatusCombo.SelectedIndex = AddressCombo.SelectedIndex = 0;
            if (id != null) Load(id.Value);
        }

        public static bool TryOpen(Window w)
        {
            if (Application.Current.Windows.OfType<OrderEditWindow>().Any())
            { MessageBox.Show(w, "Закройте окно заказа."); return false; }
            return true;
        }

        void Load(int lineId)
        {
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand("SELECT * FROM Orders WHERE order_number_id=@id", cn))
            {
                cmd.Parameters.AddWithValue("@id", lineId);
                cn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return;
                    IdBox.Text = lineId.ToString();
                    foreach (LookupItem i in ArticuleCombo.Items)
                        if (i.Text == r.GetString(2)) { ArticuleCombo.SelectedItem = i; break; }
                    Ui.ComboPick(StatusCombo, r.GetInt32(9));
                    Ui.ComboPick(AddressCombo, r.GetInt32(6));
                    OrderDate.SelectedDate = r.GetDateTime(4);
                    DeliveryDate.SelectedDate = r.GetDateTime(5);
                }
            }
        }

        void Save_Click(object sender, RoutedEventArgs e)
        {
            var art = Ui.ComboId(ArticuleCombo);
            var st = Ui.ComboId(StatusCombo);
            var ad = Ui.ComboId(AddressCombo);
            if (art == null || st == null || ad == null || !OrderDate.SelectedDate.HasValue || !DeliveryDate.SelectedDate.HasValue)
            { MessageBox.Show("Заполните поля."); return; }
            try
            {
                using (var cn = Db.Open())
                {
                    cn.Open();
                    if (_id == null)
                    {
                        int ln = (int)new SqlCommand("SELECT ISNULL(MAX(order_number_id),0)+1 FROM Orders", cn).ExecuteScalar();
                        int oid = (int)new SqlCommand("SELECT ISNULL(MAX(order_id),0)+1 FROM Orders", cn).ExecuteScalar();
                        var ins = new SqlCommand(@"INSERT INTO Orders(order_number_id,order_id,articule,amount,order_date,delivery_date,delivery_point,id_client_name,code,id_status)
VALUES(@ln,@oid,@art,1,@od,@dd,@dp,NULL,600,@st)", cn);
                        ins.Parameters.AddWithValue("@ln", ln);
                        ins.Parameters.AddWithValue("@oid", oid);
                        ins.Parameters.AddWithValue("@art", art.Text);
                        ins.Parameters.AddWithValue("@od", OrderDate.SelectedDate.Value);
                        ins.Parameters.AddWithValue("@dd", DeliveryDate.SelectedDate.Value);
                        ins.Parameters.AddWithValue("@dp", ad.Id);
                        ins.Parameters.AddWithValue("@st", st.Id);
                        ins.ExecuteNonQuery();
                    }
                    else
                    {
                        var upd = new SqlCommand(@"UPDATE Orders SET articule=@art,order_date=@od,delivery_date=@dd,delivery_point=@dp,id_status=@st WHERE order_number_id=@id", cn);
                        upd.Parameters.AddWithValue("@id", _id.Value);
                        upd.Parameters.AddWithValue("@art", art.Text);
                        upd.Parameters.AddWithValue("@od", OrderDate.SelectedDate.Value);
                        upd.Parameters.AddWithValue("@dd", DeliveryDate.SelectedDate.Value);
                        upd.Parameters.AddWithValue("@dp", ad.Id);
                        upd.Parameters.AddWithValue("@st", st.Id);
                        upd.ExecuteNonQuery();
                    }
                }
                DialogResult = true;
            }
            catch (Exception ex) { Db.Err(this, ex); }
        }
    }
}
