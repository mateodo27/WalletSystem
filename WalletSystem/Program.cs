using ConsoleTables;
using System;
using System.Data;
using WalletSystem.Core.Helper;
using WalletSystem.Core.Service;
using WalletSystem.Data.Model;

namespace WalletSystem
{
    internal class Program
    {
        private static readonly AccountService accountService = new AccountService();

        static void Main(string[] args)
        {
            DisplayVerbiage($"Welcome to {CONSTANTS.BankName}");

            try
            {
                Home();
            }
            catch (Exception ex)
            {
                // log error

                ExitPage("System Message : An unhandled error was encountered. Please restart the application or contact support service.");
            }
        }

        private static void HomeVerbiage(string errorMessage = null)
        {
            ResetPage();
            DisplayVerbiage("Please select an option");
            DisplayVerbiage("");

            if (!string.IsNullOrEmpty(errorMessage))
                DisplayErrorMessage(errorMessage);

            DisplayVerbiage("[1] Login");
            DisplayVerbiage("[2] Register");
            DisplayVerbiage("[3] Exit");
            DisplayVerbiage("");
        }

        private static void Home()
        {
            HomeVerbiage();

            while (true)
            {
                var input = ReadInput("Option : ");

                if (int.TryParse(input, out int option))
                    switch (option)
                    {
                        case 1:
                            Login();
                            break;
                        case 2:
                            Registration();
                            break;
                        case 3:
                            ExitPage("Thank you for doing business with us!");
                            Environment.Exit(0);
                            return;
                        default:
                            HomeVerbiage("Invalid selection option. Please try again.");
                            break;
                    }
                else
                    HomeVerbiage("Invalid selection option. Please try again.");
            }
        }

        private static void RegistrationVerbiage(string errorMessage = null, string username = null)
        {
            ResetPage();
            DisplayVerbiage("Please enter the following");
            DisplayVerbiage("");

            if (!string.IsNullOrEmpty(errorMessage))
                DisplayErrorMessage(errorMessage);

            if (!string.IsNullOrEmpty(username))
                DisplayVerbiage($"Username : {username}");
        }

        private static void Registration()
        {
            RegistrationVerbiage();

            string username = string.Empty;
            bool usernameAlreadyExists = false;

            do
            {
                do
                {
                    username = ReadInput("Username : ");

                    if (string.IsNullOrEmpty(username))
                        RegistrationVerbiage("Please input desired username");
                    else if (username.Contains(' '))
                    {
                        RegistrationVerbiage("Input should not contain space(s)");
                        username = string.Empty;
                    }
                }
                while (string.IsNullOrEmpty(username));

                usernameAlreadyExists = accountService.UsernameExists(username);

                if (usernameAlreadyExists)
                {
                    username = string.Empty;
                    RegistrationVerbiage("Username already exists. Please input new desired username");
                }
            }
            while (usernameAlreadyExists);

            string password = string.Empty;

            do
            {
                Console.Write("Password : ");

                ConsoleKey key;

                do
                {
                    var keyInfo = Console.ReadKey(true);
                    key = keyInfo.Key;

                    if (key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        Console.Write("\b \b");
                        password = password[0..^1];
                    }
                    else if (!char.IsControl(keyInfo.KeyChar))
                    {
                        Console.Write("*");
                        password += keyInfo.KeyChar;
                    }
                } while (key != ConsoleKey.Enter);

                if (string.IsNullOrEmpty(password))
                    RegistrationVerbiage("Please input desired password", username);
                else if (password.Contains(' '))
                {
                    RegistrationVerbiage("Input should contain space(s)");
                    password = string.Empty;
                }
            }
            while (string.IsNullOrEmpty(password));

            var account = accountService.CreateAccount(username, password);

            DisplayVerbiageWithPause("Account created! Press any key to continue");

            TransactionPage(account);
        }

        private static void LoginVerbiage(string errorMessage = null, string username = null)
        {
            ResetPage();
            DisplayVerbiage("Please enter the following");
            DisplayVerbiage("");

            if (!string.IsNullOrEmpty(errorMessage))
                DisplayErrorMessage(errorMessage);

            if (!string.IsNullOrEmpty(username))
                DisplayVerbiage($"Username : {username}");
        }

        private static void Login()
        {
            LoginVerbiage();

            string username = string.Empty;

            do
            {
                username = ReadInput("Username : ");

                if (string.IsNullOrEmpty(username))
                    LoginVerbiage("Please input your username");
            }
            while (string.IsNullOrEmpty(username));

            string password = string.Empty;

            do
            {
                Write("Password : ");

                ConsoleKey key;

                do
                {
                    var keyInfo = Console.ReadKey(true);
                    key = keyInfo.Key;

                    if (key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        Write("\b \b");
                        password = password[0..^1];
                    }
                    else if (!char.IsControl(keyInfo.KeyChar))
                    {
                        Write("*");
                        password += keyInfo.KeyChar;
                    }
                } while (key != ConsoleKey.Enter);

                if (string.IsNullOrEmpty(password))
                    LoginVerbiage("Password cannot be empty", username);
            }
            while (string.IsNullOrEmpty(password));

            var account = accountService.Login(username, password.Encrypt());

            if (account != null)
                TransactionPage(account);
            else
            {
                DisplayVerbiageWithPause("Wrong username and/or password!");
                Home();
            }
        }

        private static void TransactionVerbiage(string errorMessage = null)
        {
            ResetPage();
            DisplayVerbiage("Please select transaction");
            DisplayVerbiage("");

            if (!string.IsNullOrEmpty(errorMessage))
                DisplayErrorMessage(errorMessage);

            DisplayVerbiage("[1] Balance Inquiry");
            DisplayVerbiage("[2] Deposit");
            DisplayVerbiage("[3] Withdraw");
            DisplayVerbiage("[4] Fund Transfer");
            DisplayVerbiage("[5] Transaction History");
            DisplayVerbiage("[6] Logout");
            DisplayVerbiage("");
        }

        private static void TransactionPage(Account account)
        {
            TransactionVerbiage();

            while (true)
            {
                var input = ReadInput("Transaction Option : ");

                if (int.TryParse(input, out int option))
                    switch (option)
                    {
                        case 1:
                            BalanceInquiryPage(account);
                            break;
                        case 2:
                            DepositPage(account);
                            break;
                        case 3:
                            WithdrawalPage(account);
                            break;
                        case 4:
                            FundTransferPage(account);
                            break;
                        case 5:
                            TransactionHistoryPage(account);
                            break;
                        case 6:
                            Home();
                            return;
                        default:
                            TransactionVerbiage("Invalid selection option. Please try again.");
                            break;
                    }
                else
                    TransactionVerbiage("Invalid selection option. Please try again.");
            }
        }

        private static void BalanceInquiryVerbiage(Account account, string errorMessage = null)
        {
            ResetPage();
            AccountDisplayHeader(account);

            if (!string.IsNullOrEmpty(errorMessage))
                DisplayErrorMessage(errorMessage);
        }

        private static void BalanceInquiryPage(Account account)
        {
            BalanceInquiryVerbiage(account);

            accountService.RefreshBalance(account);

            DisplayVerbiageWithPause($"Your available balance is : {account.Balance:n}", false);

            TransactionPage(account);
        }

        private static void DepositVerbiage(Account account, string errorMessage = null)
        {
            ResetPage();
            AccountDisplayHeader(account);

            if (!string.IsNullOrEmpty(errorMessage))
                DisplayErrorMessage(errorMessage);
        }

        private static void DepositPage(Account account)
        {
            DepositVerbiage(account);

            bool isValidAmount = false;
            long amount;

            do
            {
                var input = ReadInput("Deposit Amount : ");

                if (long.TryParse(input, out amount))
                    if (amount < 0)
                        DepositVerbiage(account, "Please enter correct amount. Must be a positive amount.");
                    else if (amount == 0)
                        DepositVerbiage(account, "Please enter correct amount. Amount cannot be 0");
                    else
                        isValidAmount = true;
                else
                    DepositVerbiage(account, "Please enter correct amount.");
            }
            while (!isValidAmount);

            try
            {
                accountService.Deposit(account, amount);
                accountService.RefreshBalance(account);

                DisplayVerbiageWithPause($"Your final balance is : {account.Balance:n}");
            }
            catch (DBConcurrencyException ex)
            {
                DisplayVerbiageWithPause($"System Message : {ex.Message}");
            }
            catch (Exception ex)
            {
                DisplayVerbiageWithPause($"System Message : Transaction encountered an error. Please contact support service.");
            }
            finally
            {
                TransactionPage(account);
            }
        }

        private static void WithdrawalVerbiage(Account account, string errorMessage = null)
        {
            ResetPage();
            AccountDisplayHeader(account);

            if (!string.IsNullOrEmpty(errorMessage))
                DisplayErrorMessage(errorMessage);
        }

        private static void WithdrawalPage(Account account)
        {
            WithdrawalVerbiage(account);

            bool isValidAmount = false;
            long amount;

            do
            {
                var input = ReadInput("Withdraw Amount : ");

                if (long.TryParse(input, out amount))
                    if (amount < 0)
                        WithdrawalVerbiage(account, "Please enter correct amount. Must be a positive amount.");
                    else if (amount == 0)
                        WithdrawalVerbiage(account, "Please enter correct amount. Amount cannot be 0");
                    else if (amount > account.Balance)
                        WithdrawalVerbiage(account, "Insuficient funds. Please try other amount.");
                    else
                        isValidAmount = true;
                else
                    WithdrawalVerbiage(account, "Please enter correct amount.");
            }
            while (!isValidAmount);

            try
            {
                accountService.Withdraw(account, amount);
                accountService.RefreshBalance(account);

                DisplayVerbiageWithPause($"Your final balance is : {account.Balance:n}");
            }
            catch (DBConcurrencyException ex)
            {
                DisplayVerbiageWithPause($"System Message : {ex.Message}");
            }
            catch (Exception ex)
            {
                DisplayVerbiageWithPause($"System Message : Transaction encountered an error. Please contact support service.");
            }
            finally
            {
                TransactionPage(account);
            }
        }

        private static void FundTransferVerbiage(Account account, string errorMessage = null, string destinationAccount = null)
        {
            ResetPage();
            AccountDisplayHeader(account);

            if (!string.IsNullOrEmpty(errorMessage))
                DisplayErrorMessage(errorMessage);

            if (!string.IsNullOrEmpty(destinationAccount))
                DisplayVerbiage($"Destination Account No. : {destinationAccount}");
        }

        private static void FundTransferPage(Account account)
        {
            FundTransferVerbiage(account);

            Account receivingAccount;
            bool isValidAccountNo = false;
            long destinationAccountNo;

            do
            {
                do
                {
                    var accountNo = ReadInput("Destination Account No. : ");

                    if (long.TryParse(accountNo, out destinationAccountNo))
                        if (destinationAccountNo < 0)
                            FundTransferVerbiage(account, "Please enter correct account no.");
                        else if (destinationAccountNo == account.AccountNo)
                            FundTransferVerbiage(account, "Cannot transfer to own account");
                        else
                            isValidAccountNo = true;
                    else
                        FundTransferVerbiage(account, "Please enter a valid account no.");
                }
                while (!isValidAccountNo);

                receivingAccount = accountService.AccountExists(destinationAccountNo);

                if (receivingAccount == null)
                {
                    FundTransferVerbiage(account, "Account no. does not exists");
                    isValidAccountNo = false;
                }
            }
            while (!isValidAccountNo);

            var isValidAmount = false;
            long amount;

            do
            {
                var input = ReadInput("Transfer Amount : ");

                if (long.TryParse(input, out amount))
                    if (amount < 0)
                        FundTransferVerbiage(account, "Please enter correct amount. Must be a positive amount.");
                    else if (amount == 0)
                        FundTransferVerbiage(account, "Please enter correct amount. Amount cannot be 0");
                    else if (amount > account.Balance)
                        FundTransferVerbiage(account, "Insuficient funds. Please try other amount.");
                    else
                        isValidAmount = true;
                else
                    FundTransferVerbiage(account, "Please enter correct amount.");

            }
            while (!isValidAmount);

            try
            {
                accountService.TransferFunds(account, receivingAccount, amount);
                accountService.RefreshBalance(account);

                DisplayVerbiageWithPause($"Your final balance is : {account.Balance:n}");
            }
            catch (DBConcurrencyException ex)
            {
                DisplayVerbiageWithPause($"System Message : {ex.Message}");
            }
            catch (Exception ex)
            {
                DisplayVerbiageWithPause($"System Message : Transaction encountered an error. Please contact support service.");
            }
            finally
            {
                TransactionPage(account);
            }
        }

        private static void TransactionHistoryVerbiage(Account account, string errorMessage = null)
        {
            ResetPage();
            AccountDisplayHeader(account);

            if (!string.IsNullOrEmpty(errorMessage))
                DisplayErrorMessage(errorMessage);
        }

        private static void TransactionHistoryPage(Account account)
        {
            TransactionHistoryVerbiage(account);

            accountService.RefreshAccount(account);

            var table = new ConsoleTable("Type", "From/To Account", "Amount", "End Balance", "Date", "Time");

            foreach (var transaction in account.Transactions)
            {
                table.AddRow(
                    transaction.TransactionType,
                    transaction.FromToAccount,
                    $"{transaction.Amount:n}",
                    $"{transaction.EndBalance:n}",
                    transaction.TransactionDate.ToShortDateString(),
                    transaction.TransactionDate.ToShortTimeString());
            }

            table.Write();

            DisplayVerbiageWithPause($"Latest transactions as of {DateTime.Now}");
            TransactionPage(account);
        }

        private static void DisplayVerbiageWithPause(string text, bool spacer = true)
        {
            if (spacer)
                DisplayVerbiage("");

            DisplayVerbiage($"{text}");
            DisplayVerbiage("");
            DisplayVerbiage($"[Press any key to go back to previous page]");
            Console.ReadKey(true);
        }

        private static void DisplayErrorMessage(string errorMessage)
        {
            DisplayVerbiage($"System Message : [{errorMessage}]");
            DisplayVerbiage("");
        }

        private static void ExitPage(string text)
        {
            DisplayVerbiage("");
            DisplayVerbiage($"{text}");
            DisplayVerbiage("");
            DisplayVerbiage($"[Press any key to exit]");
            Console.ReadKey(true);
        }

        private static void ResetPage()
        {
            Console.Clear();
            DisplayHeader();
        }

        private static void DisplayHeader() => DisplayVerbiage($"***************** {CONSTANTS.BankName} *****************");

        private static void AccountDisplayHeader(Account account)
        {
            DisplayVerbiage($"Account No.  : {account.AccountNo}");
            DisplayVerbiage($"Account Name : {account.Username}");
            DisplayVerbiage("");
        }

        private static void DisplayVerbiage(string text) => Console.WriteLine(text);

        private static void Write(string text) => Console.Write(text);

        private static string ReadInput(string text)
        {
            Console.Write(text);

            return Console.ReadLine();
        }
    }
}
