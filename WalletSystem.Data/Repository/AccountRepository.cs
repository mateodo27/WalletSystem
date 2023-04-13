using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using WalletSystem.Data.Model;

namespace WalletSystem.Data.Repository
{
    public class AccountRepository
    {
        private readonly string _connectionString;

        public AccountRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["WalletSystem"].ConnectionString;
        }

        public Account GetAccount(string username, string password)
        {
            Account account = null;

            var query = @"SELECT
                              AccountNo,
                              Username,
                              Balance,
                              CreatedDate
                          FROM 
                              [dbo].[Account]
                          WHERE
                              Username = @Username
                          AND Password = @Password
";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                try
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        account = new Account
                        {
                            AccountNo = Convert.ToInt64(reader["AccountNo"]),
                            Username = username,
                            Balance = Convert.ToDecimal(reader["Balance"]),
                            CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                    }

                }
                catch (Exception ex)
                {
                    //log here

                    throw ex;
                }
                finally
                {
                    connection.Close();
                }
            }

            return account;
        }

        public Account GetAccount(long accountNo)
        {
            Account account = null;

            var query = @"SELECT
                              AccountNo,
                              Username,
                              Balance,
                              CreatedDate
                          FROM 
                              [dbo].[Account]
                          WHERE
                              AccountNo = @AccountNo
";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                try
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@AccountNo", accountNo);

                    var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        account = new Account
                        {
                            AccountNo = Convert.ToInt64(reader["AccountNo"]),
                            Username = reader["Username"].ToString(),
                            Balance = Convert.ToDecimal(reader["Balance"]),
                            CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                    };
                    }

                }
                catch (Exception ex)
                {
                    //log here

                    throw ex;
                }
                finally
                {
                    connection.Close();
                }
            }

            return account;
        }

        public void RefreshBalance(Account account)
        {
            var query = @"SELECT Balance, Version FROM [dbo].[Account] WHERE AccountNo = @AccountNo";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                try
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@AccountNo", account.AccountNo);

                    var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        account.Balance = Convert.ToDecimal(reader["Balance"]);
                    }
                }
                catch (Exception ex)
                {
                    //log here

                    throw ex;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public Account CreateAccount(string username, string password)
        {
            Account account = null;

            var query = @"EXEC [dbo].[usp_SaveAccount] @Username, @Password";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                try
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        account = new Account();

                        account.Username = username;
                        account.AccountNo = Convert.ToInt64(reader["AccountNo"]);
                        account.Balance = Convert.ToDecimal(reader["Balance"]);
                        account.CreatedDate = Convert.ToDateTime(reader["CreatedDate"]);
                    }
                }
                catch (Exception ex)
                {
                    //log here
                }
                finally
                {
                    connection.Close();
                }
            }

            return account;
        }

        public bool UsernameExists(string username)
        {
            var result = false;

            var query = @"SELECT 1 FROM [dbo].[Account] WHERE Username = @Username";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                try
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@Username", username);

                    var reader = command.ExecuteReader();

                    result = reader.HasRows;
                }
                catch (Exception ex)
                {
                    //log here

                    throw ex;
                }
                finally
                {
                    connection.Close();
                }
            }

            return result;
        }
    }
}
