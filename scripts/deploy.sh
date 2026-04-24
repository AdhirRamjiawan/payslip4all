#!/bin/sh

rm -Rf ./dist
dotnet publish Payslip4All.sln  -o ./dist -c Release
tar -cvf payslip-app.tar ./dist
scp -i ~/.ssh/payslip4all payslip-app.tar ec2-user@payslip4all.co.za:/home/ec2-user
ssh -i ~/.ssh/payslip4all ec2-user@payslip4all.co.za "bash -s" < extract_and_run.sh
