# Azure Cloud Drive Project

This repository contains the architecture, source code, and deployment configurations for a scalable, secure cloud storage application (similar to Google Drive or OneDrive) built entirely on Microsoft Azure.

## 🏗️ Architecture & Infrastructure

The application leverages a modern, serverless-first architecture on Azure to ensure high availability, global distribution, and cost efficiency.

* **Frontend:** **Azure Static Web Apps (SWA)** hosting a Single Page Application (React/Vue/Angular) with global CDN distribution.
* **Identity & Security:** **Microsoft Entra ID B2C** for user registration, social logins (Google, GitHub), and API security.
* **Compute / Backend:** **Azure Container Apps (ACA)** (for containerized microservices) or **Azure Functions** (for event-driven, serverless tasks).
* **File Storage:** **Azure Blob Storage** utilizing hot/cool access tiers for cost-effective, unstructured data storage.
* **Metadata Database:** **Azure Cosmos DB (NoSQL API)** for blazing-fast directory navigation and file tracking.
* **API Gateway:** **Azure API Management (APIM)** for rate limiting, caching, and unified routing.

---

## 🗄️ Data & Storage Implementation

This project uses the **Valet Key Pattern** to handle file transfers efficiently without bottlenecking the backend compute resources.

### The Upload/Download Flow
1. The frontend requests an upload/download URL from the API.
2. The backend generates a time-limited **Shared Access Signature (SAS) token** for Azure Blob Storage.
3. The client uploads or downloads the file *directly* to/from Blob Storage using the SAS URL.

### Cosmos DB Schema
File and folder metadata is stored in Cosmos DB to represent the virtual directory structure.

**File Document Example:**
```json
{ 
  "id": "uuid", 
  "userId": "user123", 
  "type": "file", 
  "name": "report.pdf", 
  "parentId": "folder456", 
  "size": 1024, 
  "blobUrl": "..." 
}
```

**Folder Document Example:**
```json
{ 
  "id": "folder456", 
  "userId": "user123", 
  "type": "folder", 
  "name": "Work Documents", 
  "parentId": "root" 
}
```

---

## 🔌 Backend API 

The API layer acts as the orchestrator between the frontend, Cosmos DB, and Blob Storage. All requests require a valid JWT token issued by Entra ID B2C, from which the `userId` is extracted for authorization.

### Core Endpoints

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/folders/{id}` | List the contents of a specific folder |
| `POST` | `/api/files/upload-url` | Generate a write-only SAS token for direct-to-blob upload |
| `POST` | `/api/files` | Record metadata in Cosmos DB after successful blob upload |
| `GET` | `/api/files/{id}/download-url` | Generate a read-only SAS token for downloading a file |
| `DELETE`| `/api/files/{id}` | Delete metadata from Cosmos DB and trigger blob deletion |

---

## 💻 Frontend UI

The user interface is designed to replicate a native operating system drive experience within the browser.

* **Authentication:** Managed via **MSAL.js** to handle token acquisition and silent refreshes with Entra ID B2C.
* **Components:**
  * Interactive file/folder grid and list views.
  * Drag-and-drop upload zones.
  * Real-time progress bars monitoring direct-to-blob chunked uploads.
  * Context menus (right-click) for renaming, deleting, and downloading assets.

---

## 🚀 Roadmap & Advanced Features

Future phases of this project will introduce enterprise-grade storage features:

* **Secure File Sharing:** Generate long-lived SAS tokens or internal routing links to share files publicly or with specific Entra ID users.
* **Intelligent Search:** Integration with **Azure AI Search** to index Cosmos DB metadata and perform deep-text extraction on blobs (PDFs, Word docs) for full-text search capabilities.
* **Event-Driven Workflows:** Utilizing **Azure Event Grid** to listen for new blob uploads and trigger Azure Functions for automated tasks (e.g., generating image thumbnails, scanning for malware).
