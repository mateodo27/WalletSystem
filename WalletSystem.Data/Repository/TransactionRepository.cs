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

        public decimal Deposit(long accountNo, decimal amount)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead))
                {
                    try
                    {
                        var currentBalance = GetAccountBalance(accountNo, connection, transaction);
                        var newBalance = currentBalance + amount;

                        UpdateBalance(accountNo, newBalance, connection, transaction);
                        CreateTransactionHistory(accountNo, null, amount, newBalance, TransactionType.Deposit, connection, transaction);

                        transaction.Commit();

                        return newBalance;
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

        public decimal Withdraw(long accountNo, decimal amount)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead))
                {
                    try
                    {
                        var currentBalance = GetAccountBalance(accountNo, connection, transaction);
                        var newBalance = currentBalance - amount;

                        if (newBalance >= 0)
                        {
                            UpdateBalance(accountNo, newBalance, connection, transaction);
                            CreateTransactionHistory(accountNo, null, -amount, newBalance, TransactionType.Withdrawal, connection, transaction);

                            transaction.Commit();

                            return newBalance;
                        }
                        else
                        {
                            transaction.Rollback();
                            throw new ArgumentOutOfRangeException("Insufficient funds");
                        }
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

        public decimal FundTransfer(
            long accountNo,
            long receivingAccountNo,
            decimal amount)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead))
                {
                    try
                    {
                        var currentBalance = GetAccountBalance(accountNo, connection, transaction);
                        var newBalance = currentBalance - amount;

                        if (newBalance >= 0)
                        {
                            UpdateBalance(accountNo, newBalance, connection, transaction);
                            CreateTransactionHistory(accountNo, null, -amount, newBalance, TransactionType.Transfer, connection, transaction);

                            var receivingAccountBalance = GetAccountBalance(receivingAccountNo, connection, transaction);
                            var receivingAccountNewBalance = receivingAccountBalance + amount;

                            UpdateBalance(receivingAccountNo, receivingAccountNewBalance, connection, transaction);
                            CreateTransactionHistory(receivingAccountNo, accountNo, amount, receivingAccountNewBalance, TransactionType.Transfer, connection, transaction);

                            transaction.Commit();

                            return newBalance;
                        }
                        else
                        {
                            transaction.Rollback();
                            throw new ArgumentOutOfRangeException("Insufficient funds");
                        }
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

        public decimal GetAccountBalance(long accountNo, SqlConnection connection, SqlTransaction transaction)
        {
            var lockAccount = "SELECT Balance FROM [dbo].[Account] WITH (UPDLOCK) WHERE AccountNo = @AccountNo";

            using (var lockCommand = new SqlCommand(lockAccount, connection, transaction))
            {
                lockCommand.Parameters.AddWithValue("@AccountNo", accountNo);

                using (var reader = lockCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Convert.ToDecimal(reader["Balance"]);
                    }
                    else
                    {
                        throw new Exception("Account not found");
                    }
                }
            }
        }

        private void UpdateBalance(
            long accountNo,
            decimal newBalance,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            var updateBalance = @"UPDATE [dbo].[Account] SET Balance = @NewBalance WHERE AccountNo = @AccountNo";

            using (var command = new SqlCommand(updateBalance, connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountNo", accountNo);
                command.Parameters.AddWithValue("@NewBalance", newBalance);

                var rows = command.ExecuteNonQuery();

                if (rows == 0)
                {
                    throw new DBConcurrencyException("Concurrency detected.");
                }
            }
        }

        private void CreateTransactionHistory(
            long accountNo,
            long? receivingAccountNo,
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
                command.Parameters.AddWithValue("@AccountNo", accountNo);
                if (receivingAccountNo.HasValue)
                    command.Parameters.AddWithValue("@ReceivingAccount", receivingAccountNo);
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