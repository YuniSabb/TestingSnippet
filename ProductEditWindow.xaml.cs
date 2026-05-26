using System;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace demo
{
    public partial class ProductEditWindow : Window
    {
        readonly int? _id;
        string _img, _newFile;

        public ProductEditWindow(int? id)
        {
            InitializeComponent();
            _id = id;
            Title = id == null ? "Добавление товара" : "Редактирование";
            IdLabel.Visibility = IdBox.Visibility = id == null ? Visibility.Collapsed : Visibility.Visible;
            NameCombo.ItemsSource = Db.Lookup("SELECT id_name, product_name FROM Product_name ORDER BY product_name");
            CatCombo.ItemsSource = Db.Lookup("SELECT id_category, category_name FROM Product_category ORDER BY category_name");
            ProdCombo.ItemsSource = Db.Lookup("SELECT id_producer, producer_name FROM Product_producer ORDER BY producer_name");
            DelCombo.ItemsSource = Db.Lookup("SELECT id_delivery, delivery_name FROM Product_delivery ORDER BY delivery_name");
            NameCombo.SelectedIndex = CatCombo.SelectedIndex = ProdCombo.SelectedIndex = DelCombo.SelectedIndex = 0;
            if (id != null) Load(id.Value);
            else { SaleBox.Text = "0"; StockBox.Text = "0"; Preview.Source = Img.Load(null); }
        }

        public static bool TryOpen(Window w)
        {
            if (Application.Current.Windows.OfType<ProductEditWindow>().Any())
            { MessageBox.Show(w, "Закройте окно товара."); return false; }
            return true;
        }

        void Load(int id)
        {
            using (var cn = Db.Open())
            using (var cmd = new SqlCommand("SELECT * FROM Products WHERE product_id=@id", cn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return;
                    IdBox.Text = id.ToString();
                    Ui.ComboPick(NameCombo, r.GetInt32(2));
                    ArticuleBox.Text = r.GetString(1);
                    Ui.ComboPick(CatCombo, r.GetInt32(8));
                    Ui.ComboPick(ProdCombo, r.GetInt32(6));
                    Ui.ComboPick(DelCombo, r.GetInt32(7));
                    DescBox.Text = r.IsDBNull(11) ? "" : r.GetString(11);
                    PriceBox.Text = r.GetInt32(4).ToString();
                    UnitBox.Text = r.GetString(3);
                    StockBox.Text = r.GetInt32(10).ToString();
                    SaleBox.Text = r.GetInt32(9).ToString();
                    _img = r.IsDBNull(12) ? null : r.GetString(12);
                    Preview.Source = Img.Load(_img);
                }
            }
        }

        void PickImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.jpg;*.png" };
            if (dlg.ShowDialog() != true) return;
            _newFile = dlg.FileName;
            var b = new BitmapImage();
            b.BeginInit();
            b.UriSource = new Uri(_newFile);
            b.CacheOption = BitmapCacheOption.OnLoad;
            b.EndInit();
            Preview.Source = b;
        }

        void Save_Click(object sender, RoutedEventArgs e)
        {
            var nm = Ui.ComboId(NameCombo);
            var ct = Ui.ComboId(CatCombo);
            var pr = Ui.ComboId(ProdCombo);
            var dl = Ui.ComboId(DelCombo);
            if (nm == null || ct == null || pr == null || dl == null) { MessageBox.Show("Заполните поля."); return; }
            if (!int.TryParse(PriceBox.Text, out int price) || price < 0) { MessageBox.Show("Неверная цена."); return; }
            if (!int.TryParse(StockBox.Text, out int stock) || stock < 0) { MessageBox.Show("Неверное количество."); return; }
            if (!int.TryParse(SaleBox.Text, out int sale) || sale < 0 || sale > 100) { MessageBox.Show("Скидка 0-100."); return; }
            string art = ArticuleBox.Text.Trim();
            if (art == "") { MessageBox.Show("Введите артикул."); return; }
            string file = _img;
            if (_newFile != null) { Img.Del(_img); file = Img.Save(_newFile, art); }
            try
            {
                using (var cn = Db.Open())
                {
                    cn.Open();
                    string sql = _id == null
                        ? @"INSERT INTO Products(product_id,articule,id_name,measurement,price,biggest_sale,id_producer,id_delivery,id_category,active_sale,amount_instock,description,image)
VALUES(@id,@art,@nm,@u,@p,100,@pr,@dl,@ct,@sl,@st,@ds,@im)"
                        : @"UPDATE Products SET articule=@art,id_name=@nm,measurement=@u,price=@p,id_producer=@pr,id_delivery=@dl,
id_category=@ct,active_sale=@sl,amount_instock=@st,description=@ds,image=@im WHERE product_id=@id";
                    var cmd = new SqlCommand(sql, cn);
                    int pid = _id ?? (int)new SqlCommand("SELECT ISNULL(MAX(product_id),0)+1 FROM Products", cn).ExecuteScalar();
                    cmd.Parameters.AddWithValue("@id", pid);
                    cmd.Parameters.AddWithValue("@art", art);
                    cmd.Parameters.AddWithValue("@nm", nm.Id);
                    cmd.Parameters.AddWithValue("@u", UnitBox.Text);
                    cmd.Parameters.AddWithValue("@p", price);
                    cmd.Parameters.AddWithValue("@pr", pr.Id);
                    cmd.Parameters.AddWithValue("@dl", dl.Id);
                    cmd.Parameters.AddWithValue("@ct", ct.Id);
                    cmd.Parameters.AddWithValue("@sl", sale);
                    cmd.Parameters.AddWithValue("@st", stock);
                    cmd.Parameters.AddWithValue("@ds", (object)DescBox.Text ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@im", (object)file ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
                DialogResult = true;
            }
            catch (Exception ex) { Db.Err(this, ex); }
        }
    }
}
