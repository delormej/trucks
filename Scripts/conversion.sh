#https://developers.zamzar.com/docs
key=xxxx

curl https://sandbox.zamzar.com/v1/formats/xls \
-u $key:

curl https://sandbox.zamzar.com/v1/jobs \
 -u $key: \
 -X POST \
 -F "source_file=@./settlement.xls" \
 -F "target_format=xlsx"

# Response:
{"id":8965001,"key":"$key","status":"initialising","sandbox":true,"created_at":"2019-12-09T17:39:22Z","finished_at":null,"source_file":{"id":62771565,"name":"settlement.xls","size":51200},"target_files":[],"target_format":"xlsx","credit_cost":2}%

 # Status
 curl https://sandbox.zamzar.com/v1/jobs/8965001 \
 -u $key:

 # Response:
 {"id":8965001,"key":"$key","status":"successful","sandbox":true,"created_at":"2019-12-09T17:39:22Z","finished_at":"2019-12-09T17:39:30Z","source_file":{"id":62771565,"name":"settlement.xls","size":51200},"target_files":[{"id":62771569,"name":"settlement.xlsx","size":32072}],"target_format":"xlsx","credit_cost":2}%

 # Response suggested the id is: 62771569
curl https://sandbox.zamzar.com/v1/files/62771569/content \
 -u $key: \
 -L \
 > converted.xlsx