using System;
using System.Data.SqlClient;
using System.Windows;

namespace demo
{
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();

        void Login_Click(object sender, RoutedEventArgs e)
        {
            if (LoginBox.Text.Trim() == "" || PasswordBox.Password == "")
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }
            try
            {
                using (var cn = Db.Open())
                using (var cmd = new SqlCommand("SELECT user_name, role_id FROM Users WHERE login=@l AND password=@p", cn))
                {
                    cmd.Parameters.AddWithValue("@l", LoginBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@p", PasswordBox.Password);
                    cn.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) { MessageBox.Show("Неверный логин или пароль."); return; }
                        AppSession.UserName = r.GetString(0);
                        AppSession.RoleId = r.GetInt32(1);
                    }
                }
                AppSession.OpenHome();
                Close();
            }
            catch (Exception ex) { Db.Err(this, ex); }
        }

        void Guest_Click(object sender, RoutedEventArgs e)
        {
            AppSession.UserName = null;
            AppSession.RoleId = 0;
            AppSession.OpenHome();
            Close();
        }
    }
}
