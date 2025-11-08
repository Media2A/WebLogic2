using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models.API;

namespace WebLogic.Server.Services;

/// <summary>
/// Service for API Explorer functionality
/// </summary>
public class ApiExplorerService
{
    private readonly IApiManager _apiManager;

    public ApiExplorerService(IApiManager apiManager)
    {
        _apiManager = apiManager;
    }

    /// <summary>
    /// Register API Explorer endpoints
    /// </summary>
    public void RegisterEndpoints()
    {
        // Discovery endpoint - returns JSON document
        _apiManager.RegisterEndpoint(_apiManager.CreateEndpoint()
            .Version("v1")
            .Path("/discovery")
            .Get()
            .Description("Get API discovery document")
            .Tags("system", "documentation")
            .Handler(async (req) =>
            {
                var discovery = _apiManager.GetDiscoveryDocument();

                // Create a serializable DTO without the Handler functions
                var serializableDiscovery = new
                {
                    discovery.Title,
                    discovery.Version,
                    discovery.Description,
                    discovery.Versions,
                    discovery.Tags,
                    Endpoints = discovery.Endpoints.Select(e => new
                    {
                        e.Id,
                        e.Version,
                        e.Method,
                        e.Path,
                        e.FullRoute,
                        e.Description,
                        e.Tags,
                        e.RequiresAuth,
                        e.RequiredPermissions,
                        e.IsDeprecated,
                        e.DeprecationMessage,
                        e.ExtensionId,
                        e.RateLimit,
                        RequestBodyType = e.RequestBodyType?.Name,
                        ResponseType = e.ResponseType?.Name
                    }).ToArray(),
                    discovery.GeneratedAt
                };

                return ApiResponse.Ok(serializableDiscovery);
            })
            .Build());

        // API Explorer UI endpoint - returns HTML page
        _apiManager.RegisterEndpoint(_apiManager.CreateEndpoint()
            .Version("v1")
            .Path("/explorer")
            .Get()
            .Description("Interactive API documentation")
            .Tags("system", "documentation")
            .Handler(async (req) =>
            {
                var html = GetExplorerHtml();

                // Return HTML response (not JSON)
                return new ApiResponse
                {
                    StatusCode = 200,
                    Success = true,
                    Headers = new Dictionary<string, string>
                    {
                        ["Content-Type"] = "text/html; charset=utf-8"
                    },
                    Data = html
                };
            })
            .Build());
    }

    private string GetExplorerHtml()
    {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>API Explorer - WebLogic</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: #f5f7fa;
            color: #2c3e50;
            line-height: 1.6;
        }

        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 2rem;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }

        .header h1 {
            font-size: 2rem;
            margin-bottom: 0.5rem;
        }

        .header p {
            opacity: 0.9;
        }

        .container {
            max-width: 1400px;
            margin: 0 auto;
            padding: 2rem;
        }

        .stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 1rem;
            margin-bottom: 2rem;
        }

        .stat-card {
            background: white;
            padding: 1.5rem;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }

        .stat-card h3 {
            font-size: 0.875rem;
            color: #7f8c8d;
            text-transform: uppercase;
            margin-bottom: 0.5rem;
        }

        .stat-card .value {
            font-size: 2rem;
            font-weight: bold;
            color: #667eea;
        }

        .filters {
            background: white;
            padding: 1.5rem;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            margin-bottom: 2rem;
        }

        .filters h2 {
            font-size: 1.25rem;
            margin-bottom: 1rem;
        }

        .filter-group {
            display: flex;
            gap: 1rem;
            flex-wrap: wrap;
        }

        .filter-btn {
            padding: 0.5rem 1rem;
            border: 2px solid #e1e8ed;
            background: white;
            border-radius: 20px;
            cursor: pointer;
            transition: all 0.2s;
            font-size: 0.875rem;
        }

        .filter-btn:hover {
            border-color: #667eea;
            color: #667eea;
        }

        .filter-btn.active {
            background: #667eea;
            color: white;
            border-color: #667eea;
        }

        .endpoints {
            display: grid;
            gap: 1rem;
        }

        .endpoint {
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            overflow: hidden;
            transition: all 0.2s;
        }

        .endpoint:hover {
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        }

        .endpoint-header {
            padding: 1.5rem;
            cursor: pointer;
            display: flex;
            align-items: center;
            gap: 1rem;
        }

        .method {
            padding: 0.25rem 0.75rem;
            border-radius: 4px;
            font-weight: bold;
            font-size: 0.75rem;
            text-transform: uppercase;
        }

        .method.get { background: #e3f2fd; color: #1976d2; }
        .method.post { background: #e8f5e9; color: #388e3c; }
        .method.put { background: #fff3e0; color: #f57c00; }
        .method.delete { background: #ffebee; color: #d32f2f; }
        .method.patch { background: #f3e5f5; color: #7b1fa2; }

        .endpoint-path {
            flex: 1;
            font-family: 'Courier New', monospace;
            font-size: 1rem;
        }

        .endpoint-version {
            background: #f5f7fa;
            padding: 0.25rem 0.75rem;
            border-radius: 4px;
            font-size: 0.75rem;
            font-weight: bold;
        }

        .endpoint-tags {
            display: flex;
            gap: 0.5rem;
        }

        .tag {
            background: #f5f7fa;
            padding: 0.25rem 0.75rem;
            border-radius: 4px;
            font-size: 0.75rem;
        }

        .endpoint-body {
            padding: 0 1.5rem 1.5rem;
            display: none;
            border-top: 1px solid #e1e8ed;
            padding-top: 1.5rem;
        }

        .endpoint-body.active {
            display: block;
        }

        .endpoint-description {
            margin-bottom: 1rem;
            color: #7f8c8d;
        }

        .endpoint-details {
            display: grid;
            gap: 1rem;
        }

        .detail-section {
            background: #f5f7fa;
            padding: 1rem;
            border-radius: 4px;
        }

        .detail-section h4 {
            font-size: 0.875rem;
            margin-bottom: 0.5rem;
            color: #7f8c8d;
        }

        .badge {
            display: inline-block;
            padding: 0.25rem 0.75rem;
            border-radius: 12px;
            font-size: 0.75rem;
            font-weight: bold;
        }

        .badge.auth { background: #fff3cd; color: #856404; }
        .badge.deprecated { background: #f8d7da; color: #721c24; }
        .badge.permission { background: #d1ecf1; color: #0c5460; }

        .try-it {
            margin-top: 1rem;
            padding: 1rem;
            background: #f5f7fa;
            border-radius: 4px;
        }

        .try-it h4 {
            margin-bottom: 1rem;
        }

        .input-group {
            margin-bottom: 1rem;
        }

        .input-group label {
            display: block;
            margin-bottom: 0.5rem;
            font-size: 0.875rem;
            font-weight: bold;
        }

        .input-group input,
        .input-group textarea {
            width: 100%;
            padding: 0.5rem;
            border: 1px solid #e1e8ed;
            border-radius: 4px;
            font-family: 'Courier New', monospace;
        }

        .btn {
            padding: 0.75rem 1.5rem;
            background: #667eea;
            color: white;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-weight: bold;
            transition: all 0.2s;
        }

        .btn:hover {
            background: #5568d3;
        }

        .response {
            margin-top: 1rem;
            padding: 1rem;
            background: #2c3e50;
            color: #ecf0f1;
            border-radius: 4px;
            font-family: 'Courier New', monospace;
            font-size: 0.875rem;
            white-space: pre-wrap;
            max-height: 400px;
            overflow: auto;
        }

        .loading {
            text-align: center;
            padding: 2rem;
        }

        .spinner {
            border: 3px solid #f3f3f3;
            border-top: 3px solid #667eea;
            border-radius: 50%;
            width: 40px;
            height: 40px;
            animation: spin 1s linear infinite;
            margin: 0 auto;
        }

        @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }
    </style>
</head>
<body>
    <div class=""header"">
        <h1>🚀 API Explorer</h1>
        <p>Interactive API documentation for WebLogic Server</p>
    </div>

    <div class=""container"">
        <div class=""stats"" id=""stats"">
            <div class=""stat-card"">
                <h3>Total Endpoints</h3>
                <div class=""value"" id=""total-endpoints"">0</div>
            </div>
            <div class=""stat-card"">
                <h3>Versions</h3>
                <div class=""value"" id=""total-versions"">0</div>
            </div>
            <div class=""stat-card"">
                <h3>Tags</h3>
                <div class=""value"" id=""total-tags"">0</div>
            </div>
        </div>

        <div class=""filters"">
            <h2>Filter by Version</h2>
            <div class=""filter-group"" id=""version-filters"">
                <button class=""filter-btn active"" data-filter=""all"">All Versions</button>
            </div>
        </div>

        <div class=""filters"">
            <h2>Filter by Tag</h2>
            <div class=""filter-group"" id=""tag-filters"">
                <button class=""filter-btn active"" data-filter=""all"">All Tags</button>
            </div>
        </div>

        <div id=""endpoints-container""></div>
    </div>

    <script>
        let discoveryData = null;
        let currentVersionFilter = 'all';
        let currentTagFilter = 'all';

        async function loadDiscoveryDocument() {
            try {
                const response = await fetch('/api/v1/discovery');
                discoveryData = await response.json();

                if (discoveryData.success) {
                    const data = discoveryData.data;
                    updateStats(data);
                    createFilters(data);
                    renderEndpoints(data.endpoints);
                } else {
                    console.error('Failed to load discovery document:', discoveryData.error);
                }
            } catch (error) {
                console.error('Error loading discovery document:', error);
            }
        }

        function updateStats(data) {
            document.getElementById('total-endpoints').textContent = data.endpoints.length;
            document.getElementById('total-versions').textContent = data.versions.length;
            document.getElementById('total-tags').textContent = data.tags.length;
        }

        function createFilters(data) {
            const versionFilters = document.getElementById('version-filters');
            const tagFilters = document.getElementById('tag-filters');

            // Version filters
            data.versions.forEach(version => {
                const btn = document.createElement('button');
                btn.className = 'filter-btn';
                btn.textContent = version;
                btn.dataset.filter = version;
                btn.onclick = () => filterByVersion(version);
                versionFilters.appendChild(btn);
            });

            // Tag filters
            data.tags.forEach(tag => {
                const btn = document.createElement('button');
                btn.className = 'filter-btn';
                btn.textContent = tag;
                btn.dataset.filter = tag;
                btn.onclick = () => filterByTag(tag);
                tagFilters.appendChild(btn);
            });
        }

        function filterByVersion(version) {
            currentVersionFilter = version;
            document.querySelectorAll('#version-filters .filter-btn').forEach(btn => {
                btn.classList.toggle('active', btn.dataset.filter === version || btn.dataset.filter === 'all');
            });
            applyFilters();
        }

        function filterByTag(tag) {
            currentTagFilter = tag;
            document.querySelectorAll('#tag-filters .filter-btn').forEach(btn => {
                btn.classList.toggle('active', btn.dataset.filter === tag || btn.dataset.filter === 'all');
            });
            applyFilters();
        }

        function applyFilters() {
            if (!discoveryData || !discoveryData.success) return;

            let filtered = discoveryData.data.endpoints;

            if (currentVersionFilter !== 'all') {
                filtered = filtered.filter(e => e.version === currentVersionFilter);
            }

            if (currentTagFilter !== 'all') {
                filtered = filtered.filter(e => e.tags && e.tags.includes(currentTagFilter));
            }

            renderEndpoints(filtered);
        }

        function renderEndpoints(endpoints) {
            const container = document.getElementById('endpoints-container');
            container.innerHTML = '';

            if (endpoints.length === 0) {
                container.innerHTML = '<div class=""loading""><p>No endpoints match the current filters.</p></div>';
                return;
            }

            endpoints.forEach(endpoint => {
                const endpointEl = createEndpointElement(endpoint);
                container.appendChild(endpointEl);
            });
        }

        function createEndpointElement(endpoint) {
            const div = document.createElement('div');
            div.className = 'endpoint';

            const badges = [];
            if (endpoint.requiresAuth) badges.push('<span class=""badge auth"">🔒 Auth Required</span>');
            if (endpoint.isDeprecated) badges.push('<span class=""badge deprecated"">⚠️ Deprecated</span>');
            if (endpoint.requiredPermissions && endpoint.requiredPermissions.length > 0) {
                badges.push(`<span class=""badge permission"">🔑 ${endpoint.requiredPermissions.join(', ')}</span>`);
            }

            const tags = endpoint.tags && endpoint.tags.length > 0
                ? `<div class=""endpoint-tags"">${endpoint.tags.map(t => `<span class=""tag"">${t}</span>`).join('')}</div>`
                : '';

            div.innerHTML = `
                <div class=""endpoint-header"" onclick=""toggleEndpoint(this)"">
                    <span class=""method ${endpoint.method.toLowerCase()}"">${endpoint.method}</span>
                    <span class=""endpoint-path"">${endpoint.fullRoute}</span>
                    <span class=""endpoint-version"">${endpoint.version}</span>
                </div>
                <div class=""endpoint-body"">
                    ${endpoint.description ? `<div class=""endpoint-description"">${endpoint.description}</div>` : ''}
                    ${badges.length > 0 ? `<div style=""margin-bottom: 1rem;"">${badges.join(' ')}</div>` : ''}
                    ${tags}
                    ${endpoint.isDeprecated && endpoint.deprecationMessage ? `<div class=""detail-section""><strong>Deprecation Notice:</strong> ${endpoint.deprecationMessage}</div>` : ''}
                    <div class=""try-it"">
                        <h4>Try it out</h4>
                        ${endpoint.requiresAuth ? `
                        <div class=""input-group"">
                            <label>User ID (X-User-Id header)</label>
                            <input type=""text"" class=""user-id"" placeholder=""123"" />
                        </div>
                        <div class=""input-group"">
                            <label>Permissions (X-User-Permissions header, comma-separated)</label>
                            <input type=""text"" class=""user-permissions"" placeholder=""users.read,users.write"" />
                        </div>
                        ` : ''}
                        ${endpoint.method === 'POST' || endpoint.method === 'PUT' || endpoint.method === 'PATCH' ? `
                        <div class=""input-group"">
                            <label>Request Body (JSON)</label>
                            <textarea class=""request-body"" rows=""6"">${endpoint.requestBodyType ? '{\n  \n}' : ''}</textarea>
                        </div>
                        ` : ''}
                        <button class=""btn"" onclick=""sendRequest('${endpoint.method}', '${endpoint.fullRoute}', this)"">Send Request</button>
                        <div class=""response"" style=""display: none;""></div>
                    </div>
                </div>
            `;

            return div;
        }

        function toggleEndpoint(header) {
            const body = header.nextElementSibling;
            body.classList.toggle('active');
        }

        async function sendRequest(method, path, button) {
            const parent = button.closest('.try-it');
            const responseEl = parent.querySelector('.response');
            const userIdInput = parent.querySelector('.user-id');
            const userPermissionsInput = parent.querySelector('.user-permissions');
            const bodyInput = parent.querySelector('.request-body');

            responseEl.style.display = 'block';
            responseEl.textContent = 'Sending request...';

            try {
                const headers = {
                    'Content-Type': 'application/json'
                };

                if (userIdInput && userIdInput.value) {
                    headers['X-User-Id'] = userIdInput.value;
                }

                if (userPermissionsInput && userPermissionsInput.value) {
                    headers['X-User-Permissions'] = userPermissionsInput.value;
                }

                const options = {
                    method: method,
                    headers: headers
                };

                if (bodyInput && (method === 'POST' || method === 'PUT' || method === 'PATCH')) {
                    options.body = bodyInput.value;
                }

                const response = await fetch(path, options);
                const data = await response.json();

                responseEl.textContent = JSON.stringify(data, null, 2);
            } catch (error) {
                responseEl.textContent = `Error: ${error.message}`;
            }
        }

        // Load on page load
        loadDiscoveryDocument();
    </script>
</body>
</html>";
    }
}
