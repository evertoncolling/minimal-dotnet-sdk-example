# Cognite SDK .NET Example: Fetching Time Series Data Points
This repository contains a minimal console application that demonstrates how to use the Cognite SDK for .NET to fetch time series data points from a Cognite Data Fusion (CDF) project.

## Prerequisites
Before you can run the console application, you need to create a `.env` file in the root folder of this repository and set the following variables in it (replace the `xxxx` with the correct values):
```env
TENANT_ID=xxxx
CLIENT_ID=xxxx
CDF_CLUSTER=xxxx
CDF_PROJECT=xxxx
```
## Running the Console Application
Once you have set up the `.env` file, you can run the console application by opening the terminal, navigating to the root folder of this repository, and running the following command:
```bash
dotnet run
```
This will fetch time series data points from the CDF project using the Cognite SDK for .NET.
