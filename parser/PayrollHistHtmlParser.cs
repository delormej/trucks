using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace Trucks
{
    public class PayrollHistHtmlParser
    {
        public PayrollHistHtmlParser(){}

        public List<SettlementHistory> Parse(string html)
        {
		    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            string path = "html/body/div[@class='container']/div[@class='row-fluid']/div[@class='span10']/div[@class='row-fluid']/table/tbody/tr";
            var rows = doc.DocumentNode.SelectNodes(path);
            if (rows == null)
                return null;

            List<SettlementHistory> settlements = new List<SettlementHistory>();
            foreach (var row in rows)
            {
                var nodes = row.SelectNodes("td");
                SettlementHistory settlement = ParseSettlement(nodes);
                if (settlement != null)
                    settlements.Add(settlement);
            }
            return settlements;
        }

        private SettlementHistory ParseSettlement(HtmlNodeCollection nodes)
        {
            SettlementHistory settlement = null;
            if (nodes.Count > 4 && !IsVoid(nodes[5]))
            {
                settlement = new SettlementHistory();
                settlement.SettlementId = nodes[0].InnerText;
                settlement.SettlementDate =  ParseDate(nodes[1]);
                settlement.CheckAmount = ParseDollar(nodes[2]);
                settlement.ARAmount = ParseDollar(nodes[3]);
                settlement.DeductionAmount = ParseDollar(nodes[4]);
                System.Console.WriteLine(settlement.SettlementId + ": " + settlement.CheckAmount);
            }
            else
            {
                System.Console.WriteLine("Voided");
            }            

            return settlement;
        }

        private double ParseDollar(HtmlNode node)
        {
            double result = 0.0;

            if (node != null)
            {
                string value = node.InnerText;
                if (!string.IsNullOrEmpty(value))
                {
                    if (value.StartsWith('(') && value.EndsWith(')'))
                    {
                        // trim (...) and make value negative
                        value = value.TrimStart('(').TrimEnd(')');
                        result = -1 * double.Parse(value.Substring(1, value.Length - 1));

                    }
                    result = double.Parse(value.Substring(1, value.Length - 1));
                }
            }

            return result;
        }

        private DateTime ParseDate(HtmlNode node)
        {
            DateTime? result = null;
            if (node != null)
            {
                string value = node.InnerText;
                if (!string.IsNullOrEmpty(value))
                    result = DateTime.Parse(value);
            }

            if (result == null)
                throw new ApplicationException("Unable to parse date from node.");                
            
            return (DateTime)result;
        }        

        private bool IsVoid(HtmlNode node)
        {
            bool isVoid = false;
            if (node != null)
            {
                string value = node.InnerText.Trim();
                isVoid = !string.IsNullOrWhiteSpace(value);
            }

            return isVoid;
        }
    }
}