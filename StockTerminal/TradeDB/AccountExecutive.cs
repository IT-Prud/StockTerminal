using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class AccountExecutive
    {
        public string Code;
        public string Name;

        public AccountExecutive(string Code, string Name)
        {
            this.Code = Code;
            this.Name = Name;
        }

        public void Update(AccountExecutive NewAccountExecutive)
        {
            Code = NewAccountExecutive.Code;
            Name = NewAccountExecutive.Name;
        }

        public AccountExecutive Clone()
        {
            return new AccountExecutive(Code, Name);
        }

        public override string ToString()
        {
            return Code + " " + Name;
        }
    }
}
