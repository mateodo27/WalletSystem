using System;
using System.Collections.Generic;
using System.Text;
using static WalletSystem.Data.Helper.Enums;

namespace WalletSystem.Data.Model
{
    public class Transaction
    {
        public int TransactionId { get; set; }

        public long? FromToAccount { get; set; }

        public TransactionType TransactionType { get; set; }

        public decimal Amount { get; set; }

        public decimal EndBalance { get; set; }

        public DateTime TransactionDate { get; set; }
    }
}
