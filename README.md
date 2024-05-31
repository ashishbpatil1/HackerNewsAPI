# HackerNewsAPI Project

## Overview

This project is an ASP.NET Core API that fetches and serves HackerNews stories. It provides endpoints to retrieve the latest stories (configurable) and story details.

## Prerequisites

- [.NET SDK 8](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)

## Getting Started

1. **Clone the Repository:**
   Use following link to clone the app in visual studio:
   https://github.com/ashishbpatil1/hackernews-api.git

2. **Restore dependencies:**
   dotnet restore

3. **Update Configuration:**
   Edit the appsettings.json file to configure your settings if necessary.

4. **Run the Application:**
   dotnet run

   The API will be available at https://localhost:5001 (or http://localhost:5000).

**API Endpoints:**
**Get Top Stories**
Endpoint: GET /api/story
Description: Retrieves the top 200 story details from HackerNews.
Response: A list of story details.

**Caching**
The API uses in-memory caching to store the top stories for a specified duration. The cache is invalidated and refreshed when a new HTTP call happens.

**Error Handling**
The API handles various exceptions and logs errors for debugging purposes.

**Unit Tests**
To run the unit tests, use the following command:
dotnet test

**Swagger UI**
Swagger UI is enabled for this project. To access the API documentation, navigate to 
'[This URL](https://localhost:7102/swagger/ui/index.html)' in your browser.

**License**
This project is licensed under the MIT License.


