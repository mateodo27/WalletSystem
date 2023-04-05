using NUnit.Framework;
using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
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
        private string _username = "username";
        private string _password = "password";

        [SetUp]
        public void Setup()
        {
            _accountService = new AccountService();
            _connectionString = ConfigurationManager.ConnectionStrings["WalletSystem"].ConnectionString;
        }

        [Test]
        public void CanCreateUser()
        {
            CleanUp();

            var account = CreateUserAccount(_username, _password);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.Username == _username);
            Assert.IsTrue(account.Balance == 0);
        }

        [Test]
        public void CanLogin()
        {
            CleanUp();

            var account = CreateUserAccount(_username, _password);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.Username == _username);

            var loginAccount = _accountService.Login(_username, _password);

            Assert.IsNotNull(loginAccount);
            Assert.IsTrue(loginAccount.Username == _username);
        }

        [Test]
        public void CanValidateThatUsernameAlreadyExists()
        {
            CleanUp();

            var account = CreateUserAccount(_username, _password);

            Assert.IsNotNull(account);

            var exists = _accountService.UsernameExists(_username);

            Assert.IsTrue(exists);
        }

        [Test]
        public void CanValidateThatAccountExists()
        {
            CleanUp();

            var account = CreateUserAccount(_username, _password);

            Assert.IsNotNull(account);

            var createdAccount = _accountService.AccountExists(account.AccountNo);

            Assert.IsNotNull(createdAccount);
        }

        [Test]
        public void CanDeposit()
        {
            CleanUp();

            var amount = 50000;
            var account = CreateUserAccount(_username, _password);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.Balance == 0);

            _accountService.Deposit(account, amount);
            _accountService.RefreshAccount(account);

            Assert.IsTrue(account.Balance == amount);
        }

        [Test]
        public void CanWithdraw()
        {
            var amountToDeposit = 50000;
            var amountToWithdraw = 30000;
            var expectedEndBalance = amountToDeposit - amountToWithdraw;

            CleanUp();

            var account = CreateUserAccount(_username, _password);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.Balance == 0);

            _accountService.Deposit(account, amountToDeposit);
            _accountService.RefreshAccount(account);

            Assert.IsTrue(account.Balance == amountToDeposit);

            _accountService.Withdraw(account, amountToWithdraw);
            _accountService.RefreshAccount(account);

            Assert.IsTrue(account.Balance == expectedEndBalance);
        }

        [Test]
        public void CanTransferFunds()
        {
            var amountToDeposit = 50000;
            var amountToTransfer = 15000;
            var expectedEndBalance = amountToDeposit - amountToTransfer;

            var receiverUsername = "receiver";
            var receiverPassword = "password";

            CleanUp(receiverUsername);
            CleanUp();

            var account = CreateUserAccount(_username, _password);
            var receivingAccount = CreateUserAccount(receiverUsername, receiverPassword);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.Balance == 0);
            Assert.IsTrue(receivingAccount.Balance == 0);

            _accountService.Deposit(account, amountToDeposit);
            _accountService.RefreshBalance(account);

            Assert.IsTrue(account.Balance == amountToDeposit);

            _accountService.TransferFunds(account, receivingAccount, amountToTransfer);
            _accountService.RefreshBalance(account);
            _accountService.RefreshBalance(receivingAccount);

            Assert.IsTrue(account.Balance == expectedEndBalance);
            Assert.IsTrue(receivingAccount.Balance == amountToTransfer);
        }

        [Test]
        public void DepositConcurrencyTest()
        {
            CleanUp();

            var account = CreateUserAccount(_username, _password);

            Task desposit1 = Task.Factory.StartNew(() => _accountService.Deposit(account, 25000));
            Task desposit2 = Task.Factory.StartNew(() => _accountService.Deposit(account, 10000));

            try
            {
                Task.WaitAll(desposit1, desposit2);

                Assert.IsTrue(false);
            }
            catch (Exception ex) when (ex.InnerException is DBConcurrencyException)
            {
                Assert.IsTrue(true);
            }
        }

        [Test]
        public void WithdrawalAndDepositConcurrencyTest()
        {
            CleanUp();

            var account = CreateUserAccount(_username, _password);

            Task deposit = Task.Factory.StartNew(() => _accountService.Deposit(account, 25000));
            Task withdraw = Task.Factory.StartNew(() => _accountService.Withdraw(account, 20000));

            try
            {
                Task.WaitAll(deposit, withdraw);

                Assert.IsTrue(false);
            }
            catch (Exception ex) when (ex.InnerException is DBConcurrencyException)
            {
                Assert.IsTrue(true);
            }
        }

        [Test]
        public void WithdrawConcurrencyTest()
        {
            CleanUp();

            var account = CreateUserAccount(_username, _password);

            _accountService.Deposit(account, 50000);
            _accountService.RefreshAccount(account);

            Task withdraw1 = Task.Factory.StartNew(() => _accountService.Withdraw(account, 25000));
            Task withdraw2 = Task.Factory.StartNew(() => _accountService.Withdraw(account, 10000));

            try
            {
                Task.WaitAll(withdraw1, withdraw2);

                Assert.IsTrue(false);
            }
            catch (Exception ex) when (ex.InnerException is DBConcurrencyException)
            {
                Assert.IsTrue(true);
            }
        }

        [Test]
        public void TransferFundsConcurrencyTest_TransferingAccountUpdated()
        {
            var receiverUsername = "receiver";
            var receiverPassword = "password";

            CleanUp(receiverUsername);
            CleanUp();

            var account = CreateUserAccount(_username, _password);
            var receivingAccount = CreateUserAccount(receiverUsername, receiverPassword);

            _accountService.Deposit(account, 50000);

            Task deposit = Task.Factory.StartNew(() => _accountService.Deposit(account, 20000));
            Task transfer1 = Task.Factory.StartNew(() => _accountService.TransferFunds(account, receivingAccount, 20000));

            try
            {
                Task.WaitAll(deposit, transfer1);

                Assert.IsTrue(false);
            }
            catch (Exception ex) when (ex.InnerException is DBConcurrencyException)
            {
                Assert.IsTrue(true);
            }
        }

        [Test]
        public void TransferFundsConcurrencyTest_MultiTransfer()
        {
            var receiverUsername = "receiver";
            var receiverPassword = "password";

            CleanUp(receiverUsername);
            CleanUp();

            var account = CreateUserAccount(_username, _password);
            var receivingAccount = CreateUserAccount(receiverUsername, receiverPassword);

            _accountService.Deposit(account, 50000);
            _accountService.RefreshBalance(account);

            Task transfer1 = Task.Factory.StartNew(() => _accountService.TransferFunds(account, receivingAccount, 20000));
            Task transfer2 = Task.Factory.StartNew(() => _accountService.TransferFunds(account, receivingAccount, 10000));

            try
            {
                Task.WaitAll(transfer1, transfer2);

                Assert.IsTrue(false);
            }
            catch (Exception ex) when (ex.InnerException is DBConcurrencyException)
            {
                Assert.IsTrue(true);
            }
        }

        [Test]
        public void TransferFundsConcurrencyTest_ReceivingAccountUpdated()
        {
            var receiverUsername = "receiver";
            var receiverPassword = "password";

            CleanUp(receiverUsername);
            CleanUp();

            var account = CreateUserAccount(_username, _password);
            var receivingAccount = CreateUserAccount(receiverUsername, receiverPassword);

            Task deposit = Task.Factory.StartNew(() => _accountService.Deposit(receivingAccount, 10000));
            Task transfer = Task.Factory.StartNew(() => _accountService.TransferFunds(account, receivingAccount, 25000));

            try
            {
                Task.WaitAll(deposit, transfer);

                Assert.IsTrue(false);
            }
            catch (Exception ex) when (ex.InnerException is DBConcurrencyException)
            {
                Assert.IsTrue(true);
            }
        }

        public void CleanUp(string username = null)
        {
            var query = $@"DECLARE @AccountNo BIGINT = (SELECT AccountNo FROM [dbo].[Account] WHERE Username = '{(string.IsNullOrEmpty(username) ? _username : username)}')
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