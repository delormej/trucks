import sys

class Truck:
	def __init__(self):
		self.purchaseMonth = 0
		self.ageInMonthsAtPurchase = 0 # implies this object will calculate age?
		self.costToBuy = 32000
		# allow for purchasing used trucks

	def Age(self, month):
		return (month - self.purchaseMonth) + self.ageInMonthsAtPurchase

class Parameters:
	def __init__(self):
		self.weeklyNetRevenue = 250
		self.costToBuy = 32000
		self.initialTrucks = 1

def Maintenance(ageInMonths):
	year = ageInMonths // 12
	if year > 7:
		return 0
	maintenance = 1170
	repairs = [0, 150, 1200, 2400, 1000, 1500, 2500, 3500 ]
	return (repairs[year] + maintenance) / 12

def Income(ageInMonths, weeklyNet):
	year = ageInMonths // 12
	if year > 7:
		return 0
	turnoverCost = [ 0, 1000, 500, 1000, 1250, 1500, 2000, 3000 ]
	return (weeklyNet * 4) - (turnoverCost[year] / 12)

def Residual(ageInMonths):
	year = ageInMonths // 12
	if year >= 7:
		return 0
	residuals = [ 22000, 20000, 13000, 9000, 3000, 1500, 500 ]
	return residuals[year]

def GetTotalResidual(trucks, months):
	residual = 0
	for truck in trucks:
		residual += Residual(truck.Age(months))
	return residual

def GetMonthlyTruckRevenue(month, weeklyNetRevenue, trucks):
	revenue = 0
	for truck in trucks:
		age = truck.Age(month)
		revenue += Income(age, weeklyNetRevenue) - Maintenance(age)
	return revenue

def GetActiveTrucks(trucks, month):
	activeTrucks = 0
	for truck in trucks:
		age = truck.Age(month)
		if (age <= (12*7)):
			activeTrucks += 1
	return activeTrucks	

def GetLastYearRevenue(months, weeklyNetRevenue, trucks):
	total = 0
	for month in range(months, months-12, -1):
		total += GetMonthlyTruckRevenue(month, weeklyNetRevenue, trucks)
	return total

def Calculate(months, params):
	balance = 0
	trucks = []
	for truckCount in range(0, params.initialTrucks):
		trucks.append(Truck())

	monthly = []
	for month in range(1, months+1):
		revenue = GetMonthlyTruckRevenue(month, params.weeklyNetRevenue, trucks)
		monthly.append({"month": month, "revenue": revenue, "trucks": GetActiveTrucks(trucks, month)})
		# buy a new truck each month if you can:
		balance += revenue
		if balance >= params.costToBuy:
			t = Truck()
			t.purchaseMonth = month
			trucks.append(t)
			balance -= params.costToBuy

	residual = GetTotalResidual(trucks, months)
	balance += residual
	activeTrucks = GetActiveTrucks(trucks, months)
	lastYearRevenue = GetLastYearRevenue(months, params.weeklyNetRevenue, trucks)
	for r in monthly:
		f = ("%s, %s, %s" % (r["month"], r["revenue"], r["trucks"]))
		print f
	return (balance, lastYearRevenue, activeTrucks)

def main(initialTrucks):
  params = Parameters()
  params.initialTrucks = initialTrucks
  print Calculate(12*10, params)
  
if __name__== "__main__":
	if (len(sys.argv) > 1):
		main(int(sys.argv[1]))
	else:
		print "Error in usage"
