#! /usr/bin/bash
echo "Updating Studio Online..."
echo "Getting latest"
git pull
echo "Starting Server"
(dotnet run &)