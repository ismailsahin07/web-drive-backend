## Phase 1: Architecture & Infrastructure Design

Before writing any code, map out the Azure services that will power your application.

*   **Front End: Azure Static Web Apps (SWA).** This is perfect for hosting a React, Vue, or Angular Single Page Application (SPA). It provides global CDN distribution and seamless integration with GitHub Actions for deployment.
*   **Identity & Access Management (IAM): Microsoft Entra ID (formerly Azure AD) B2C.** This handles user registration, login (including social logins like Google or GitHub), and secures your APIs.
*   **Compute (Backend APIs): Azure Container Apps (ACA) or Azure Functions.**
    *   **Azure Container Apps:** Use ACA if you prefer building microservices (e.g., using Node.js, .NET, or Go) packaged in Docker containers. It’s excellent for scaling based on HTTP traffic or event triggers.
    *   **Azure Functions:** Use Functions for a purely serverless approach, which is often cheaper and simpler if your backend primarily consists of independent, short-lived tasks (like generating pre-signed URLs or processing webhooks).
*   **Storage (Files): Azure Blob Storage.** This is the core of your "drive." It is highly scalable and cost-effective for unstructured data like documents, images, and videos. You will use a combination of hot and cool access tiers.
*   **Database (Metadata): Azure Cosmos DB.** You need a database to store file metadata (filename, size, owner ID, folder structure, tags, creation date). Cosmos DB (NoSQL API) provides single-digit millisecond response times, making directory navigation blazing fast.
*   **API Gateway (Optional but Recommended): Azure API Management (APIM).** If your backend grows complex, APIM sits in front of your APIs to handle rate limiting, caching, and unified routing.

---

## Phase 2: Core Data & Storage Implementation

Start by building the foundation: where files live and how they are tracked.

1.  **Set up Azure Storage Account:** Create a Blob Storage account. Define a container strategy (e.g., one container per tenant/user, or a shared container with virtual directories based on user IDs).
2.  **Implement the Upload/Download Pattern (Crucial Step):** Do not route file uploads through your backend API (Compute). This chokes your servers. Instead, implement the Valet Key pattern:
    *   The frontend requests an upload URL from your backend.
    *   The backend generates a Shared Access Signature (SAS) token for Azure Blob Storage with a short expiration (e.g., 5 minutes) and specific permissions (write-only).
    *   The frontend uploads the file directly to Blob Storage using the SAS URL.
3.  **Design the Cosmos DB Schema:** Design your JSON documents to represent files and folders.
    *   **Example File Document:** 
        ```json
        { "id": "uuid", "userId": "user123", "type": "file", "name": "report.pdf", "parentId": "folder456", "size": 1024, "blobUrl": "..." }
        ```
    *   **Example Folder Document:** 
        ```json
        { "id": "folder456", "userId": "user123", "type": "folder", "name": "Work Documents", "parentId": "root" }
        ```

---

## Phase 3: Backend API Development

Build the API layer that manages the business logic and connects the frontend to Cosmos DB and Blob Storage.

1.  **Authentication Integration:** Configure Entra ID B2C. Ensure your API requires a valid JWT token for every request and extracts the `userId` from the token to authorize data access.
2.  **Develop Core Endpoints:**
    *   `GET /api/folders/{id}` (List contents of a folder)
    *   `POST /api/files/upload-url` (Generate SAS token for upload)
    *   `POST /api/files` (Record metadata in Cosmos DB after a successful direct-to-blob upload)
    *   `GET /api/files/{id}/download-url` (Generate read-only SAS token for download)
    *   `DELETE /api/files/{id}` (Delete metadata and trigger blob deletion)
3.  **Containerize (If using ACA):** Write a Dockerfile for your API and test it locally.

---

## Phase 4: Frontend Development (Static Web App)

Build the user interface that looks and feels like a drive.

1.  **Setup & Routing:** Initialize your SPA framework. Set up routing (e.g., `/drive/my-files`, `/drive/shared`).
2.  **Integrate Authentication:** Use MSAL.js (Microsoft Authentication Library) to handle the login flow with Entra ID B2C and manage token refresh.
3.  **Build the UI Components:**
    *   A file/folder tree or grid view.
    *   Drag-and-drop upload zones.
    *   Progress bars for uploads (monitoring the direct-to-blob transfer).
    *   Context menus (right-click) for rename, delete, and download.
4.  **Connect to APIs:** Implement the API calls, ensuring the JWT token is attached to the `Authorization` header.

---

## Phase 5: Advanced Features & Refinement

Once the core is working, add features that make it a true "drive" product.

*   **File Sharing:** Allow users to generate long-lived SAS tokens (or unique backend links) to share files publicly or with specific Entra ID users.
*   **Search Integration:** Use Azure AI Search (formerly Cognitive Search). It can index your Cosmos DB metadata and even crack open blobs (like PDFs and Word docs) to provide full-text search across the drive.
*   **Event-Driven Processing:** Use Azure Event Grid. When a new blob is uploaded, Event Grid can trigger an Azure Function to generate thumbnails for images or scan for malware.
