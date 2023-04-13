using System;
using System.Collections.Generic;
using System.Text;

namespace WalletSystem.Data.Model
{
    public class Account
    {
        public long AccountNo { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public decimal Balance { get; set; }

        public DateTime CreatedDate { get; set; }

        public List<Transaction> Transactions { get; set; } 

        public bool HasTransactions => Transactions != null && Transactions.Count > 0;
    }
}
