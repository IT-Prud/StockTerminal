using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class StockRelationBook
    {
        private volatile Dictionary<string, StockRelation> TheStockRelated = new Dictionary<string, StockRelation>();

        public void LoadFromCsv(string Csv)
        {
            if (Csv == null || Csv.Length <= 0)
                return;

            Dictionary<string, StockRelation> newStockRelated = new Dictionary<string, StockRelation>(5000);

            string[] lines = Csv.Split('\n');
            int lineCount = lines.Length;

            string[] fields;
            int fieldCount;

            StockRelation stkRelation = null;
            StockRelation.StockRelationStock shortStk, fromStk;
            string exCode, stkCode, exCodeFrom, stkCodeFrom;
            string sign, signFrom;

            for (int i = 0; i < lineCount; i++)
            {
                fields = lines[i].Split(',');
                fieldCount = fields.Length;

                if (fieldCount == 14)
                {
                    exCode = fields[0].Trim().ToUpper();
                    stkCode = fields[1].Trim().ToUpper();
                    exCodeFrom = fields[6].Trim().ToUpper();
                    stkCodeFrom = fields[7].Trim().ToUpper();

                    if (exCode != null && exCode.Length > 0 && stkCode != null && stkCode.Length > 0 && exCodeFrom != null && exCodeFrom.Length > 0 && stkCodeFrom != null && stkCodeFrom.Length > 0)
                    {
                        sign = Stock.GetSignature(exCode, stkCode);
                        signFrom = Stock.GetSignature(exCodeFrom, stkCodeFrom);

                        shortStk = new StockRelation.StockRelationStock();
                        shortStk.StockSignature = sign;
                        shortStk.InstrumentType = fields[2].Trim().ToUpper();
                        shortStk.ProductType = fields[3].Trim();
                        shortStk.Currency = fields[4].Trim().ToUpper();
                        decimal.TryParse(fields[5].Trim(), out shortStk.LotSize);
                        shortStk.Weight = 1;

                        fromStk = new StockRelation.StockRelationStock();
                        fromStk.StockSignature = signFrom;
                        fromStk.InstrumentType = fields[8].Trim().ToUpper();
                        fromStk.ProductType = fields[9].Trim();
                        fromStk.Currency = fields[10].Trim().ToUpper();
                        decimal.TryParse(fields[11].Trim(), out fromStk.LotSize);
                        decimal.TryParse(fields[12].Trim(), out fromStk.Weight);

                        if (shortStk.InstrumentType.Length > 0 && shortStk.ProductType.Length > 0 && shortStk.Currency.Length > 0 && shortStk.LotSize > 0 && shortStk.Weight > 0 &&
                            fromStk.InstrumentType.Length > 0 && fromStk.ProductType.Length > 0 && fromStk.Currency.Length > 0 && fromStk.LotSize > 0 && fromStk.Weight > 0)
                        {
                            if (stkRelation == null || stkRelation.Short == null || stkRelation.Short.StockSignature != sign)
                            {
                                if (!newStockRelated.TryGetValue(sign, out stkRelation))
                                    newStockRelated.Add(sign, stkRelation = new StockRelation());
                            }

                            stkRelation.Short = shortStk;
                            stkRelation.From.Add(fromStk);
                            stkRelation.Relation = fields[13].Trim();
                        }
                    }
                }
            }

            TheStockRelated = newStockRelated;
        }

        public StockRelation GetStockRelated(string StockSignature)
        {
            StockRelation stkRelation = null;

            if (StockSignature != null)
                TheStockRelated.TryGetValue(StockSignature, out stkRelation);

            return stkRelation;
        }
    }
}
