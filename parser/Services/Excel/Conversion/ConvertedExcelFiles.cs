using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Trucks
{
    public class ProcessedSettlement
    {
        public string SettlementId { get; set; }
        public int CompanyId { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime ProcessedTimestamp { get; set; }
    }

    public class ProcessedSettlementEventArgs : EventArgs
    {
        public ProcessedSettlementEventArgs(ProcessedSettlement settlement)
        {
            this.settlement = settlement;
        }

        public ProcessedSettlement settlement { get; set; }
    }

    public class ConvertedExcelFiles
    {
        ExcelConverter _converter;
        SettlementRepository _repository;



        public ConvertedExcelFiles(ExcelConverter converter)
        {
            this._converter = converter;
            _repository = new SettlementRepository();
        }

        private SettlementHistory GetBySettlementId(IEnumerable<SettlementHistory> settlements, string settlementId)
        {
            return settlements.Where(s => 
                s.SettlementId == settlementId).FirstOrDefault();            
        }
    }
}