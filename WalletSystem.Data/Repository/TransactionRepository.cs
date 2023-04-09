using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using WalletSystem.Data.Model;
using static WalletSystem.Data.Helper.Enums;

namespace WalletSystem.Data.Repository
{
    public class TransactionRepository
    {
        private readonly string _connectionString;

        public TransactionRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["WalletSystem"].ConnectionString;
        }

        public void Deposit(Account account, decimal amount)
        {
            var query = @"EXEC [dbo].[usp_Deposit] @AccountNo, @Balance, @RowVersion, @Amount";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                try
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@AccountNo", account.AccountNo);
                    command.Parameters.AddWithValue("@Balance", account.Balance);
                    command.Parameters.AddWithValue("@RowVersion", account.Version);
                    command.Parameters.AddWithValue("@Amount", amount);

                    var rows = command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Data has been modified")
                    {
                        throw new DBConcurrencyException(ex.Message);
                    }
                    else
                    {
                        throw ex;
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public void Withdraw(Account account, decimal amount)
        {
            var query = @"EXEC [dbo].[usp_Withdraw] @AccountNo, @Balance, @RowVersion, @Amount";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                try
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@AccountNo", account.AccountNo);
                    command.Parameters.AddWithValue("@Balance", account.Balance);
                    command.Parameters.AddWithValue("@RowVersion", account.Version);
                    command.Parameters.AddWithValue("@Amount", amount);

                    var rows = command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Data has been modified")
                    {
                        throw new DBConcurrencyException(ex.Message);
                    }
                    else
                    {
                        throw ex;
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public void FundTransfer(Account account, Account ReceivingAccount, decimal amount)
        {
            var query = @"EXEC [dbo].[usp_TransferFunds] 
                            @AccountNo, 
                            @Balance, 
                            @RowVersion, 
                            @DestinationAccountNo, 
                            @DestinationBalance, 
                            @DestinationRowVersion, 
                            @Amount";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                try
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@AccountNo", account.AccountNo);
                    command.Parameters.AddWithValue("@Balance", account.Balance);
                    command.Parameters.AddWithValue("@RowVersion", account.Version);
                    command.Parameters.AddWithValue("@DestinationAccountNo", ReceivingAccount.AccountNo);
                    command.Parameters.AddWithValue("@DestinationBalance", ReceivingAccount.Balance);
                    command.Parameters.AddWithValue("@DestinationRowVersion", ReceivingAccount.Version);
                    command.Parameters.AddWithValue("@Amount", amount);

                    var rows = command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Data has been modified")
                    {
                        throw new DBConcurrencyException(ex.Message);
                    }
                    else
                    {
                        throw ex;
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public List<Model.Transaction> GetTransactions(long accountNo)
        {
            var transactions = new List<Model.Transaction>();

            var query = @"SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
                          SELECT
                          	  TransactionId,
                          	  FromToAccount,
                          	  TransactionTypeId,
                          	  Amount,
                          	  EndBalance,
                          	  TransactionDate
                          FROM
                          	  [dbo].[Transaction]
                          WHERE
                          	  AccountNo = @AccountNo";

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
                        var transaction = new Model.Transaction();

                        transaction.TransactionId = Convert.ToInt16(reader["TransactionId"]);
                        transaction.TransactionType = (TransactionType)reader["TransactionTypeId"];
                        transaction.Amount = Convert.ToDecimal(reader["Amount"]);
                        transaction.EndBalance = Convert.ToDecimal(reader["EndBalance"]);
                        transaction.TransactionDate = Convert.ToDateTime(reader["TransactionDate"]);

                        if (reader["FromToAccount"] != DBNull.Value)
                            transaction.FromToAccount = Convert.ToInt64(reader["FromToAccount"]);

                        transactions.Add(transaction);
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

            return transactions;
        }
    }
}