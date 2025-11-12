using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace WpfApp3
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=localhost;Database=application;Uid=root;";

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private string ShowInputDialog(string text)
        {
            Window prompt = new Window()
            {
                Width = 300,
                Height = 150,
                Title = "Jelszó ellenőrzése",
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            StackPanel panel = new StackPanel() { Margin = new Thickness(10) };
            TextBlock lbl = new TextBlock() { Text = text, Margin = new Thickness(0, 0, 0, 10) };
            PasswordBox input = new PasswordBox() { Margin = new Thickness(0, 0, 0, 10) };
            Button ok = new Button() { Content = "OK", Width = 80, IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right };
            ok.Click += (sender, e) => prompt.DialogResult = true;

            panel.Children.Add(lbl);
            panel.Children.Add(input);
            panel.Children.Add(ok);
            prompt.Content = panel;

            if (prompt.ShowDialog() == true)
                return input.Password;
            else
                return string.Empty;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string version = txtVersion.Text;
            string user = txtUserName.Text;
            string pass = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Minden mezőt ki kell tölteni!");
                return;
            }

            string salt = Guid.NewGuid().ToString();
            string hash = HashPassword(pass, salt);

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT INTO datas (Version, UserName, Password, Salt, RegTime, ModTime) " +
                             "VALUES (@v,@u,@p,@s,NOW(),NOW())";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@v", version);
                cmd.Parameters.AddWithValue("@u", user);
                cmd.Parameters.AddWithValue("@p", hash);
                cmd.Parameters.AddWithValue("@s", salt);
                cmd.ExecuteNonQuery();
            }

            LoadData();
            ClearInputs();
        }

        private void LoadData()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                MySqlDataAdapter da = new MySqlDataAdapter("SELECT Id, Version, UserName, Password, Salt, RegTime, ModTime FROM datas", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dataGrid.ItemsSource = dt.DefaultView;
            }
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is DataRowView row)
            {
                string user = row["UserName"].ToString();
                string salt = row["Salt"].ToString();
                string storedHash = row["Password"].ToString();

                string input = ShowInputDialog($"Add meg a(z) {user} jelszavát:");

                bool match = HashPassword(input, salt) == storedHash;

                MessageBox.Show(match ? "A jelszó helyes ✅" : "Helytelen jelszó ❌");
            }
            else
            {
                MessageBox.Show("Válassz ki egy sort a táblázatból!");
            }
        }

        private string HashPassword(string password, string salt)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password + salt);
                return Convert.ToBase64String(sha.ComputeHash(bytes));
            }
        }

        private void ClearInputs()
        {
            txtVersion.Clear();
            txtUserName.Clear();
            txtPassword.Clear();
        }
    }
}
