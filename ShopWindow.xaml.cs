using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

namespace demo
{
    public partial class ShopWindow : Window
    {
        readonly List<ProductRow> _all = new List<ProductRow>();

        public ShopWindow()
        {
            InitializeComponent();
            FioText.Text = AppSession.IsGuest ? "Гость" : AppSession.UserName ?? "";
            if (AppSession.IsAdmin) { AdminBar.Visibility = OrderAdminBar.Visibility = OrdersTab.Visibility = Visibility.Visible; }
            else if (AppSession.RoleId == 3) OrdersTab.Visibility = Visibility.Visible;

            if (AppSession.IsStaff)
            {
                FilterBar.Visibility = Visibility.Visible;
                SortCombo.Items.Add("По умолчанию");
                SortCombo.Items.Add("На складе ↑");
                SortCombo.Items.Add("На складе ↓");
                SortCombo.SelectedIndex = 0;
                SearchBox.TextChanged += (s, e) => ShowProducts();
                SupplierCombo.SelectionChanged += (s, e) => ShowProducts();
                SortCombo.SelectionChanged += (s, e) => ShowProducts();
            }
            LoadProducts();
            if (AppSession.IsStaff) LoadOrders();
        }

        void LoadProducts()
        {
            _all.Clear();
            try
            {
                using (var cn = Db.Open())
                {
                    cn.Open();
                    var cmd = new SqlCommand(@"SELECT p.product_id,p.articule,p.price,p.biggest_sale,p.active_sale,p.amount_instock,
p.measurement,p.description,p.image,n.product_name,c.category_name,pr.producer_name,d.delivery_name
FROM Products p JOIN Product_name n ON p.id_name=n.id_name JOIN Product_category c ON p.id_category=c.id_category
JOIN Product_producer pr ON p.id_producer=pr.id_producer JOIN Product_delivery d ON p.id_delivery=d.id_delivery", cn);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) _all.Add(ProductRow.Read(r));
                }
                if (AppSession.IsStaff)
                {
                    SupplierCombo.Items.Clear();
                    SupplierCombo.Items.Add("Все поставщики");
                    foreach (var s in _all.Select(p => p.Supplier).Distinct().OrderBy(x => x)) SupplierCombo.Items.Add(s);
                    SupplierCombo.SelectedIndex = 0;
                }
                ShowProducts();
            }
            catch (Exception ex) { Db.Err(this, ex); }
        }

        void ShowProducts()
        {
            IEnumerable<ProductRow> q = _all;
            if (AppSession.IsStaff)
            {
                string s = (SearchBox.Text ?? "").Trim().ToLower();
                if (s != "")
                    q = q.Where(p => (p.Name + p.Category + p.Description + p.Producer + p.Supplier + p.Articule + p.Unit).ToLower().Contains(s));
                if (SupplierCombo.SelectedIndex > 0)
                    q = q.Where(p => p.Supplier == SupplierCombo.SelectedItem.ToString());
                if (SortCombo.SelectedIndex == 1) q = q.OrderBy(p => p.Stock);
                else if (SortCombo.SelectedIndex == 2) q = q.OrderByDescending(p => p.Stock);
                else q = q.OrderBy(p => p.Name);
            }
            else q = q.OrderBy(p => p.Name);
            ProductsList.ItemsSource = q.ToList();
        }

        void LoadOrders()
        {
            try
            {
                using (var cn = Db.Open())
                {
                    cn.Open();
                    var cmd = new SqlCommand(@"SELECT o.order_number_id,o.order_id,o.articule,o.amount,s.status_name,a.adress,o.order_date,o.delivery_date,o.code
FROM Orders o JOIN Orders_status s ON o.id_status=s.id_status JOIN Delivery_adress a ON o.delivery_point=a.id_adress", cn);
                    var list = new List<OrderRow>();
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new OrderRow
                            {
                                LineId = r.GetInt32(0), OrderNum = r.GetInt32(1), Articule = r.GetString(2), Amount = r.GetInt32(3),
                                Status = r.GetString(4), Address = r.GetString(5), OrderDate = r.GetDateTime(6),
                                DeliveryDate = r.GetDateTime(7), Code = r.GetInt32(8)
                            });
                    OrdersGrid.ItemsSource = list;
                }
            }
            catch (Exception ex) { Db.Err(this, ex); }
        }

        void Logout_Click(object sender, RoutedEventArgs e) => AppSession.Logout(this);

        void ProductsList_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!AppSession.IsAdmin) return;
            EditProduct((ProductsList.SelectedItem as ProductRow)?.ProductId);
        }

        void AddProduct_Click(object sender, RoutedEventArgs e) => EditProduct(null);

        void EditProduct(int? id)
        {
            if (!ProductEditWindow.TryOpen(this)) return;
            if (new ProductEditWindow(id).ShowDialog() == true) LoadProducts();
        }

        void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            var row = ProductsList.SelectedItem as ProductRow;
            if (row == null) { MessageBox.Show("Выберите товар."); return; }
            if (MessageBox.Show("Удалить?", "", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                using (var cn = Db.Open())
                {
                    cn.Open();
                    if ((int)new SqlCommand("SELECT COUNT(*) FROM Orders WHERE articule=@a", cn) { Parameters = { new SqlParameter("@a", row.Articule) } }.ExecuteScalar() > 0)
                    { MessageBox.Show("Товар в заказе."); return; }
                    string img = new SqlCommand("SELECT image FROM Products WHERE product_id=@id", cn) { Parameters = { new SqlParameter("@id", row.ProductId) } }.ExecuteScalar() as string;
                    new SqlCommand("DELETE FROM Products WHERE product_id=@id", cn) { Parameters = { new SqlParameter("@id", row.ProductId) } }.ExecuteNonQuery();
                    Img.Del(img);
                }
                LoadProducts();
            }
            catch (Exception ex) { Db.Err(this, ex); }
        }

        void OrdersGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!AppSession.IsAdmin) return;
            EditOrder((OrdersGrid.SelectedItem as OrderRow)?.LineId);
        }

        void AddOrder_Click(object sender, RoutedEventArgs e) => EditOrder(null);

        void EditOrder(int? id)
        {
            if (!OrderEditWindow.TryOpen(this)) return;
            if (new OrderEditWindow(id).ShowDialog() == true) LoadOrders();
        }

        void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            var row = OrdersGrid.SelectedItem as OrderRow;
            if (row == null) { MessageBox.Show("Выберите заказ."); return; }
            if (MessageBox.Show("Удалить?", "", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                using (var cn = Db.Open())
                {
                    cn.Open();
                    new SqlCommand("DELETE FROM Orders WHERE order_number_id=@id", cn) { Parameters = { new SqlParameter("@id", row.LineId) } }.ExecuteNonQuery();
                }
                LoadOrders();
            }
            catch (Exception ex) { Db.Err(this, ex); }
        }
    }
}
