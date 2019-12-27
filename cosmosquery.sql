SELECT 
        s.id AS SettlementId,
        s.SettlementDate,
        c.TruckId,
        SUM(c.TotalPaid) AS TotalPaid,
        SUM(c.Miles) AS Miles,
        SUM(d.TotalDeductions) AS TotalDeductions
    FROM SettlementHistory s
    JOIN c IN s.Credits
    JOIN d IN s.Deductions
GROUP BY s.id, s.SettlementDate, c.TruckId