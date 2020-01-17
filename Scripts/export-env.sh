cat .env | while read assignment
do
  export "${assignment}"
done