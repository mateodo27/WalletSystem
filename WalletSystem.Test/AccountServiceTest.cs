using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using WalletSystem.Core.Service;
using WalletSystem.Data.Model;
using WalletSystem.Data.Repository;

namespace WalletSystem.Test
{
    public class Tests
    {
        private string _connectionString { get; set; }
        private AccountService _accountService;

        private string _accountNo = string.Empty;

        [SetUp]
        public void Setup()
        {
            _accountService = new AccountService();
            _connectionString = ConfigurationManager.ConnectionStrings["WalletSystem"].ConnectionString;
        }

        [Test]
        public void CanCreateUser()
        {
            var username = "utc1";
            var password = "123";

            CleanUp(username);

            var account = CreateUserAccount(username, password);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.Username == username);
            Assert.IsTrue(account.Balance == 0);
        }

        [Test]
        public void CanLogin()
        {
            var username = "utc2";
            var password = "123";

            CleanUp(username);

            var account = CreateUserAccount(username, password);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.Username == username);

            var loginAccount = _accountService.Login(username, password);

            Assert.IsNotNull(loginAccount);
            Assert.IsTrue(loginAccount.Username == username);
        }

        [Test]
        public void CanValidateThatUsernameAlreadyExists()
        {
            var username = "utc3";
            var password = "123";

            CleanUp(username);

            var account = CreateUserAccount(username, password);

            Assert.IsNotNull(account);

            var exists = _accountService.UsernameExists(username);

            Assert.IsTrue(exists);
        }

        [Test]
        public void CanValidateThatAccountExists()
        {
            var username = "utc4";
            var password = "123";

            CleanUp(username);

            var account = CreateUserAccount(username, password);

            Assert.IsNotNull(account);

            var createdAccount = _accountService.AccountExists(account.AccountNo);

            Assert.IsNotNull(createdAccount);
        }

        [Test]
        public void CanDeposit()
        {
            var username = "utc5";
            var password = "123";

            CleanUp(username);

            var amount = 50000;
            var account = CreateUserAccount(username, password);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.Balance == 0);

            _accountService.Deposit(account, amount);

            Assert.IsTrue(account.Balance == amount);
        }

        [Test]
        public void CanWithdraw()
        {
            var username = "utc6";
            var password = "123";

            CleanUp(username);

            var account = CreateUserAccount(username, password);

            var amountToDeposit = 50000;
            var amountToWithdraw = 30000;
            var expectedEndBalance = amountToDeposit - amountToWithdraw;

            Assert.IsNotNull(account);
            Assert.IsTrue(account.Balance == 0);

            _accountService.Deposit(account, amountToDeposit);

            Assert.IsTrue(account.Balance == amountToDeposit);

            _accountService.Withdraw(account, amountToWithdraw);

            Assert.IsTrue(account.Balance == expectedEndBalance);
        }

        [Test]
        public void CanTransferFunds()
        {
            var username = "utc7";
            var password = "123";

            var amountToDeposit = 50000;
            var amountToTransfer = 15000;
            var expectedEndBalance = amountToDeposit - amountToTransfer;

            var receiverUsername = "utc7_receiver";
            var receiverPassword = "password";

            CleanUp(receiverUsername);
            CleanUp(username);

            var account = CreateUserAccount(username, password);
            var receivingAccount = CreateUserAccount(receiverUsername, receiverPassword);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.Balance == 0);
            Assert.IsTrue(receivingAccount.Balance == 0);

            _accountService.Deposit(account, amountToDeposit);

            Assert.IsTrue(account.Balance == amountToDeposit);

            _accountService.TransferFunds(account, receivingAccount, amountToTransfer);

            Assert.IsTrue(account.Balance == expectedEndBalance);
            Assert.IsTrue(receivingAccount.Balance == amountToTransfer);
        }

        [Test]
        public void DepositConcurrencyTest()
        {
            var username = "utc8";
            var password = "123";

            CleanUp(username);

            var account = CreateUserAccount(username, password);

            var numberOfThreads = 100;
            var amount = 1000;
            var totalAmount = amount * numberOfThreads;

            Parallel.ForEach(Enumerable.Range(0, numberOfThreads), (_) =>
            {
                _accountService.Deposit(account, amount);
            });

            _accountService.RefreshBalance(account);

            Assert.IsTrue(account.Balance == totalAmount);
        }

        [Test]
        public void WithdrawConcurrencyTest()
        {
            var username = "utc9";
            var password = "123";

            CleanUp(username);

            var account = CreateUserAccount(username, password);

            var initialDeposit = 500000;
            _accountService.Deposit(account, initialDeposit);

            var numberOfThreads = 75;
            var amount = 1200;
            var totalAmount = amount * numberOfThreads;
            var expectedEndBalance = initialDeposit - totalAmount;

            Parallel.ForEach(Enumerable.Range(0, numberOfThreads), (_) =>
            {
                _accountService.Withdraw(account, amount);
            });

            _accountService.RefreshBalance(account);

            Assert.IsTrue(account.Balance == expectedEndBalance);
        }

        [Test]
        public void TransferFundsConcurrencyTest()
        {
            var username = "utc10";
            var password = "123";

            var receiverUsername = "utc10_receiver";
            var receiverPassword = "password";

            CleanUp(receiverUsername);
            CleanUp(username);

            var account = CreateUserAccount(username, password);
            var receivingAccount = CreateUserAccount(receiverUsername, receiverPassword);

            var initialDeposit = 500000;
            _accountService.Deposit(account, initialDeposit);

            var numberOfThreads = 150;
            var amount = 1500;
            var totalAmount = amount * numberOfThreads;
            var expectedEndBalance = initialDeposit - totalAmount;

            Parallel.ForEach(Enumerable.Range(0, numberOfThreads), (_) =>
            {
                _accountService.TransferFunds(account, receivingAccount, amount);
            });

            _accountService.RefreshBalance(account);
            _accountService.RefreshBalance(receivingAccount);

            Assert.IsTrue(account.Balance == expectedEndBalance);
            Assert.IsTrue(receivingAccount.Balance == totalAmount);
        }


        [Test]
        public void MultipleTransactionsConcurrencyTest()
        {
            var username = "utc10";
            var password = "123";

            var receiverUsername = "utc10_receiver";
            var receiverPassword = "password";

            CleanUp(receiverUsername);
            CleanUp(username);

            var account = CreateUserAccount(username, password);
            var receivingAccount = CreateUserAccount(receiverUsername, receiverPassword);

            var initialDeposit = 500000;
            _accountService.Deposit(account, initialDeposit);

            var numberOfThreads = 150;
            var amountToDeposit = 1500;
            var amountToWithdraw = 1000;
            var amountToTransfer = 750;
            var totalDeposit = (amountToDeposit * numberOfThreads) + initialDeposit;
            var totalWithdrawal = (amountToWithdraw * numberOfThreads);
            var totalTransfer = (amountToTransfer * numberOfThreads);
            var totalDeductedAmount = totalWithdrawal + totalTransfer;
            var expectedEndBalance = totalDeposit - totalDeductedAmount;

            Parallel.ForEach(Enumerable.Range(0, numberOfThreads), (_) =>
            {
                _accountService.Deposit(account, amountToDeposit);
                _accountService.Withdraw(account, amountToWithdraw);
                _accountService.TransferFunds(account, receivingAccount, amountToTransfer);
            });

            _accountService.RefreshBalance(account);
            _accountService.RefreshBalance(receivingAccount);

            Assert.IsTrue(account.Balance == expectedEndBalance);
            Assert.IsTrue(receivingAccount.Balance == totalTransfer);
        }


        public void CleanUp(string username)
        {
            var query = $@"DECLARE @AccountNo BIGINT = (SELECT AccountNo FROM [dbo].[Account] WHERE Username = '{username}')
                           DELETE FROM [dbo].[Transaction] WHERE AccountNo = @AccountNo OR FromToAccount = @AccountNo
                           DELETE FROM [dbo].[Account] WHERE AccountNo = @AccountNo";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();
                var reader = command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public Account CreateUserAccount(string username, string password)
            => _accountService.CreateAccount(username, password);
    }
}