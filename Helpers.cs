using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace demo
{
    static class Db
    {
        public static SqlConnection Open() => new SqlConnection(
            ConfigurationManager.ConnectionStrings["Db"].ConnectionString);

        public static List<LookupItem> Lookup(string sql)
        {
            var list = new List<LookupItem>();
            using (var cn = Open())
            {
                cn.Open();
                using (var r = new SqlCommand(sql, cn).ExecuteReader())
                    while (r.Read()) list.Add(new LookupItem { Id = r.GetInt32(0), Text = r.GetString(1) });
            }
            return list;
        }

        public static int Scalar(string sql)
        {
            using (var cn = Open())
            {
                cn.Open();
                return (int)new SqlCommand(sql, cn).ExecuteScalar();
            }
        }

        public static void Err(Window w, Exception ex) =>
            MessageBox.Show(w, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    static class Img
    {
        static string Dir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

        public static BitmapImage Load(string file)
        {
            string p = string.IsNullOrWhiteSpace(file) ? null : Path.Combine(Dir, file.Trim());
            if (p == null || !File.Exists(p)) p = Path.Combine(Dir, "picture.png");
            if (!File.Exists(p)) return null;
            var i = new BitmapImage();
            i.BeginInit();
            i.CacheOption = BitmapCacheOption.OnLoad;
            i.UriSource = new Uri(p, UriKind.Absolute);
            i.EndInit();
            i.Freeze();
            return i;
        }

        public static string Save(string src, string art)
        {
            Directory.CreateDirectory(Dir);
            string dest = Path.Combine(Dir, art + ".jpg");
            using (var s = System.Drawing.Image.FromFile(src))
            using (var b = new Bitmap(300, 200))
            using (var g = Graphics.FromImage(b))
            {
                g.DrawImage(s, 0, 0, 300, 200);
                b.Save(dest, ImageFormat.Jpeg);
            }
            return art + ".jpg";
        }

        public static void Del(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) return;
            string p = Path.Combine(Dir, file);
            if (File.Exists(p)) try { File.Delete(p); } catch { }
        }
    }

    sealed class ProductRow
    {
        public int ProductId, BasePrice, Discount, Stock;
        public string Articule, Name, Category, Description, Producer, Supplier, Unit;
        public decimal FinalPrice;
        public BitmapImage Photo;
        public bool HasDiscount => Discount > 0 && FinalPrice < BasePrice;
        public bool IsOutOfStock => Stock <= 0;
        public bool IsHighDiscount => Stock > 0 && Discount > 15;
        public string TitleLine => Category + " | " + Name;
        public string DiscountLabel => Discount > 0 ? Discount + "%" : "—";

        public static ProductRow Read(SqlDataReader r)
        {
            int price = r.GetInt32(2), pct = r.GetInt32(4) > 0 ? r.GetInt32(3) : 0;
            if (pct > 100) pct = 100;
            int stock = r.GetInt32(5);
            return new ProductRow
            {
                ProductId = r.GetInt32(0),
                Articule = r.GetString(1),
                BasePrice = price,
                Discount = pct,
                FinalPrice = price * (100 - pct) / 100m,
                Stock = stock,
                Unit = r.GetString(6),
                Description = r.IsDBNull(7) ? "" : r.GetString(7),
                Photo = Img.Load(r.IsDBNull(8) ? null : r.GetString(8)),
                Name = r.GetString(9),
                Category = r.GetString(10),
                Producer = r.GetString(11),
                Supplier = r.GetString(12)
            };
        }
    }

    sealed class OrderRow
    {
        public int LineId, OrderNum, Amount, Code;
        public string Articule, Status, Address;
        public DateTime OrderDate, DeliveryDate;
    }

    sealed class LookupItem
    {
        public int Id;
        public string Text;
        public override string ToString() => Text;
    }

    static class Ui
    {
        public static void ComboPick(ComboBox cb, int id)
        {
            foreach (LookupItem i in cb.Items)
                if (i.Id == id) { cb.SelectedItem = i; return; }
        }

        public static LookupItem ComboId(ComboBox cb) => cb.SelectedItem as LookupItem;
    }

    public static class AppSession
    {
        public static string UserName;
        public static int RoleId;
        public static bool IsGuest => RoleId == 0;
        public static bool IsAdmin => RoleId == 1;
        public static bool IsStaff => RoleId == 1 || RoleId == 3;

        public static void OpenHome() { new ShopWindow().Show(); }

        public static void Logout(Window w)
        {
            UserName = null;
            RoleId = 0;
            new MainWindow().Show();
            w.Close();
        }
    }
}
