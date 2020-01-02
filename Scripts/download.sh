POST http://fleetweb.pantherpremium.com/Login/Login HTTP/1.1
Host: fleetweb.pantherpremium.com
Connection: keep-alive
Content-Length: 46
Cache-Control: max-age=0
Origin: http://fleetweb.pantherpremium.com
Upgrade-Insecure-Requests: 1
Content-Type: application/x-www-form-urlencoded
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3
Referer: http://fleetweb.pantherpremium.com/Login
Accept-Encoding: gzip, deflate
Accept-Language: en-US,en;q=0.9
Cookie: _ga=GA1.2.1518291896.1575903031; _gid=GA1.2.728365851.1575903031; _fbp=fb.1.1575903031470.1560151406; _gat_gtag_UA_109148056_1=1

UserID=53357&Password=PANTHER&RememberMe=false

# Parse cookie from login:
Set-Cookie: session-id=7xUeMqsRibefKhMdChfT; expires=Tue, 10-Dec-2019 01:42:24 GMT; path=/



# Get a list of settlement history check #s
curl -i -H "Cookie: session-id=hkmP4Appn50ao5ouWF3g" http://fleetweb.pantherpremium.com/Financial/PayrollHist

curl -s -H "Cookie: session-id=7xUeMqsRibefKhMdChfT" http://fleetweb.pantherpremium.com/Financial/DownloadSettlementReport?ChkNo=CD623730 --output CD623730.xls 

# Each settlment (check details) has this:
<td><a class="open-ViewCheckDialog btn btn-link" data-target="#checkDetailsModal" data-toggle="modal" data-id="CD450949">CD450949</a></td>
<td>11/16/2012</td>
<td>$2,083.69</td>
<td>$1,551.91</td>
<td>($531.78)</td>
<td>
</td>
