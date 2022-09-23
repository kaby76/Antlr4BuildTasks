#!/usr/bin/bash
tests=`find _tests -name test.sh`
for i in "$tests"
do
	bash "$i"
done
