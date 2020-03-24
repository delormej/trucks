using System;
using System.IO;

namespace Trucks
{
    public class SettlementFile
    {
        public int CompanyId;
        public string SettlementId;
        public string Filename { get; private set; }

        public static SettlementFile FromFilename(string filename)
        {
            string error = $"{filename} is not a proper settlement filename.";

            string file = Path.GetFileNameWithoutExtension(filename);
            if (file == null)
                throw new ArgumentException(error);
            
            string[] parts = file.Split("_");
            if (parts.Length < 2)
                throw new ArgumentException(error);
            
            int companyId;
            if (!int.TryParse(parts[0], out companyId))
                throw new ArgumentException(error);

            string settlementId = parts[1];

            return new SettlementFile {
                Filename = filename,
                CompanyId = companyId,
                SettlementId = settlementId
            };
        }

        public static SettlementFile FromValues(string companyId, string settlementId, bool converted)
        {
            string extension = converted ? "xlsx" : "xls";
            string filename = $"{companyId}_{settlementId}.{extension}";
            return new SettlementFile() {
                CompanyId = int.Parse(companyId),
                SettlementId = settlementId,
                Filename = filename
            };
        }
    }
}