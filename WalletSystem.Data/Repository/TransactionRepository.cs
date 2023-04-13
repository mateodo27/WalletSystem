using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Security.Principal;
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
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    try
                    {
                        GetAccount(account, connection, transaction);

                        var newBalance = account.Balance + amount;

                        UpdateBalance(account, newBalance, connection, transaction);
                        CreateTransactionHistory(account, null, amount, newBalance, TransactionType.Deposit, connection, transaction);

                        transaction.Commit();

                        account.Balance = newBalance;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        public void Withdraw(Account account, decimal amount, decimal newBalance)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    try
                    {
                        GetAccount(account, connection, transaction);
                        UpdateBalance(account, newBalance, connection, transaction);
                        CreateTransactionHistory(account, null, amount, newBalance, TransactionType.Withdrawal, connection, transaction);

                        transaction.Commit();

                        account.Balance = newBalance;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        public void FundTransfer(
            Account account,
            decimal transferingAmount,
            decimal accountNewBalance,
            Account receivingAccount,
            decimal receivingAmount,
            decimal receivingAccountNewBalance)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    try
                    {
                        GetAccount(account, connection, transaction);
                        UpdateBalance(account, accountNewBalance, connection, transaction);
                        CreateTransactionHistory(account, receivingAccount, transferingAmount, accountNewBalance, TransactionType.Transfer, connection, transaction);

                        GetAccount(receivingAccount, connection, transaction);
                        UpdateBalance(receivingAccount, receivingAccountNewBalance, connection, transaction);
                        CreateTransactionHistory(receivingAccount, account, receivingAmount, receivingAccountNewBalance, TransactionType.Transfer, connection, transaction);

                        transaction.Commit();

                        account.Balance = accountNewBalance;
                        receivingAccount.Balance = receivingAccountNewBalance;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }


        public void GetAccount(Account account, SqlConnection connection, SqlTransaction transaction)
        {
            var lockAccount = "SELECT * FROM [dbo].[Account] WITH (UPDLOCK, ROWLOCK) WHERE AccountNo = @AccountNo AND Balance = @Balance";

            using (var lockCommand = new SqlCommand(lockAccount, connection, transaction))
            {
                lockCommand.Parameters.AddWithValue("@AccountNo", account.AccountNo);
                lockCommand.Parameters.AddWithValue("@Balance", account.Balance);

                var reader = lockCommand.ExecuteReader();

                if (reader.Read())
                {
                    reader.Close();
                }
                else
                {
                    reader.Close();
                    throw new DBConcurrencyException("Balance has been updated. Please refresh.");
                }
            }
        }

        private void UpdateBalance(
            Account account,
            decimal newBalance,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            var updateBalance = @"UPDATE [dbo].[Account] SET Balance = @NewBalance WHERE AccountNo = @AccountNo";

            using (var command = new SqlCommand(updateBalance, connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountNo", account.AccountNo);
                command.Parameters.AddWithValue("@NewBalance", newBalance);

                var rows = command.ExecuteNonQuery();

                if (rows == 0)
                {
                    throw new DBConcurrencyException("Concurrency detected.");
                }
            }
        }

        private void CreateTransactionHistory(
            Account account,
            Account receivingAccount,
            decimal amount,
            decimal endBalance,
            TransactionType type,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            var tranasctionHistoryQuery = @"INSERT INTO [dbo].[Transaction]
	                                        (
	                                        	  AccountNo,
	                                        	  FromToAccount,
	                                        	  TransactionTypeId,
	                                        	  Amount,
	                                        	  EndBalance,
	                                        	  TransactionDate
	                                        )
	                                        VALUES
	                                        (
	                                        	  @AccountNo,
	                                        	  @ReceivingAccount,
	                                        	  @TransactionId,
	                                        	  @Amount,
	                                        	  @EndBalance,
	                                        	  @TransactionDate
	                                        )";

            using (var command = new SqlCommand(tranasctionHistoryQuery, connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountNo", account.AccountNo);
                if (receivingAccount != null)
                    command.Parameters.AddWithValue("@ReceivingAccount", receivingAccount.AccountNo);
                else
                    command.Parameters.AddWithValue("@ReceivingAccount", DBNull.Value);
                command.Parameters.AddWithValue("@TransactionId", (int)type);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@EndBalance", endBalance);
                command.Parameters.AddWithValue("@TransactionDate", DateTime.Now);

                var rows = command.ExecuteNonQuery();
            }
        }

        public List<Model.Transaction> GetTransactions(long accountNo)
        {
            var transactions = new List<Model.Transaction>();

            var query = @"SELECT
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