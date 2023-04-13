﻿using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using WalletSystem.Core.Helper;
using WalletSystem.Data.Model;
using WalletSystem.Data.Repository;
using static WalletSystem.Data.Helper.Enums;

namespace WalletSystem.Core.Service
{
    public class AccountService
    {
        private readonly AccountRepository _accountRepository;
        private readonly TransactionRepository _transactionRepository;

        public AccountService()
        {
            _accountRepository = new AccountRepository();
            _transactionRepository = new TransactionRepository();
        }

        public Account CreateAccount(string username, string password)
        {
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                return _accountRepository.CreateAccount(username, password.Encrypt());
            else
                return null;
        }

        public Account Login(string username, string password) => _accountRepository.GetAccount(username, password.Encrypt());

        public void RefreshAccount(Account account)
        {
            _accountRepository.RefreshBalance(account);

            account.Transactions = _transactionRepository.GetTransactions(account.AccountNo);
        }

        public bool UsernameExists(string username) => _accountRepository.UsernameExists(username);

        public Account AccountExists(long accountNo) => _accountRepository.GetAccount(accountNo);

        public void RefreshBalance(Account account) => _accountRepository.RefreshBalance(account);

        public void Deposit(Account account, decimal amount) => 
            _transactionRepository.Deposit(
                account, 
                amount); 

        public void Withdraw(Account account, decimal amount) => 
            _transactionRepository.Withdraw(
                account, 
                -amount, 
                account.Balance + -amount);

        public void TransferFunds(Account account, Account receivingAccount, decimal amount) =>
            _transactionRepository.FundTransfer(
                account,
                -amount,
                account.Balance + -amount, 
                receivingAccount,
                amount,
                receivingAccount.Balance + amount);
    }
}
