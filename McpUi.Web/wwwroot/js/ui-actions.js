// UI Actions Handler for MCP UI Client
(function () {
    'use strict';
    
    // Handle UI actions from iframes
    window.handleUIAction = function(action) {
        console.log('UI Action received:', action);
        
        // Validate action structure
        if (!action || !action.type) {
            console.error('Invalid UI action: missing type');
            showError('Invalid UI action: missing type');
            return;
        }
        
        switch (action.type) {
            case 'tool':
                handleToolAction(action.payload);
                break;
            case 'notify':
                handleNotifyAction(action.payload);
                break;
            case 'link':
                handleLinkAction(action.payload);
                break;
            default:
                console.warn('Unknown UI action type:', action.type);
                showError(`Unknown action type: ${action.type}`);
                break;
        }
    };
    
    // Handle tool action
    function handleToolAction(payload) {
        if (!payload || !payload.toolName) {
            console.error('Invalid tool action: missing toolName');
            showError('Invalid tool action: missing toolName');
            return;
        }
        
        console.log('Executing tool:', payload.toolName, 'with params:', payload.params);
        
        // Add to log console
        addToLog('tool', `Executing tool: ${payload.toolName}`);
        
        // Show loading state
        showLoading(`Executing tool: ${payload.toolName}`);
        
        // Prepare the request
        const requestData = {
            name: payload.toolName,
            arguments: payload.params || {}
        };
        
        // Get session ID from a global variable or hidden field
        const sessionId = window.currentSessionId || getQueryParam('session_id') || '';
        if (sessionId) {
            requestData.session_id = sessionId;
        }
        
        // Call the backend API
        fetch(`/mcp/tools/${payload.toolName}/call`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(requestData)
        })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            console.log('Tool execution result:', data);
            hideLoading();
            
            // Show result to user
            showResult(`Tool '${payload.toolName}' executed successfully`, data);
            
            // Notify the iframe of success if needed
            // This would depend on the specific implementation
        })
        .catch(error => {
            console.error('Tool execution failed:', error);
            hideLoading();
            showError(`Tool '${payload.toolName}' failed: ${error.message}`);
        });
    }
    
    // Handle notify action
    function handleNotifyAction(payload) {
        if (!payload || !payload.message) {
            console.error('Invalid notify action: missing message');
            return;
        }
        
        console.log('Notification:', payload.message);
        showNotification(payload.message);
    }
    
    // Handle link action
    function handleLinkAction(payload) {
        if (!payload || !payload.url) {
            console.error('Invalid link action: missing URL');
            showError('Invalid link action: missing URL');
            return;
        }
        
        console.log('Opening link:', payload.url);
        
        // Add to log console
        addToLog('link', `Attempting to open link: ${payload.url}`);
        
        // Check if URL is allowed based on domain policy
        if (isUrlAllowed(payload.url)) {
            window.open(payload.url, '_blank');
            addToLog('link', `Opened link: ${payload.url}`);
        } else {
            const message = `Link blocked by policy: ${payload.url}. Only links to allowed domains can be opened.`;
            showError(message);
            addToLog('error', message);
        }
    }
    
    // Check if URL is allowed based on domain allowlist policy
    function isUrlAllowed(url) {
        try {
            const urlObj = new URL(url);
            
            // Only allow http and https URLs
            if (urlObj.protocol !== 'http:' && urlObj.protocol !== 'https:') {
                return false;
            }
            
            // Get allowed domains from a global configuration or default to common domains
            const allowedDomains = window.uiActionAllowedDomains || [
                'localhost',
                '127.0.0.1',
                'example.com',
                'github.com',
                'microsoft.com',
                'google.com'
            ];
            
            // Check if the domain is in the allowlist
            const hostname = urlObj.hostname.toLowerCase();
            return allowedDomains.some(domain => 
                hostname === domain.toLowerCase() || 
                hostname.endsWith('.' + domain.toLowerCase())
            );
        } catch (e) {
            console.error('Error validating URL:', e);
            return false;
        }
    }
    
    // Initialize default allowed domains
    window.uiActionAllowedDomains = window.uiActionAllowedDomains || [
        'localhost',
        '127.0.0.1',
        'example.com'
    ];
    
    // Function to configure allowed domains
    window.setAllowedDomains = function(domains) {
        if (Array.isArray(domains)) {
            window.uiActionAllowedDomains = domains;
            console.log('Allowed domains updated:', domains);
            addToLog('notification', `Allowed domains updated: ${domains.join(', ')}`);
        } else {
            console.error('Invalid domains configuration: must be an array');
            showError('Invalid domains configuration: must be an array');
        }
    };
    
    // Function to add a domain to the allowlist
    window.addAllowedDomain = function(domain) {
        if (typeof domain === 'string' && domain.length > 0) {
            if (!window.uiActionAllowedDomains.includes(domain)) {
                window.uiActionAllowedDomains.push(domain);
                console.log('Domain added to allowlist:', domain);
                addToLog('notification', `Domain added to allowlist: ${domain}`);
            }
        } else {
            console.error('Invalid domain: must be a non-empty string');
            showError('Invalid domain: must be a non-empty string');
        }
    };
    
    // Function to remove a domain from the allowlist
    window.removeAllowedDomain = function(domain) {
        if (typeof domain === 'string' && domain.length > 0) {
            const index = window.uiActionAllowedDomains.indexOf(domain);
            if (index > -1) {
                window.uiActionAllowedDomains.splice(index, 1);
                console.log('Domain removed from allowlist:', domain);
                addToLog('notification', `Domain removed from allowlist: ${domain}`);
            }
        } else {
            console.error('Invalid domain: must be a non-empty string');
            showError('Invalid domain: must be a non-empty string');
        }
    };
    
    // Function to get current allowed domains
    window.getAllowedDomains = function() {
        return [...window.uiActionAllowedDomains]; // Return a copy
    };
    
    // Function to check if a domain is allowed
    window.isDomainAllowed = function(domain) {
        if (typeof domain !== 'string' || domain.length === 0) {
            return false;
        }
        const lowerDomain = domain.toLowerCase();
        return window.uiActionAllowedDomains.some(allowedDomain => 
            lowerDomain === allowedDomain.toLowerCase() || 
            lowerDomain.endsWith('.' + allowedDomain.toLowerCase())
        );
    };
    
    // Log initial configuration
    console.log('UI Actions Handler initialized with allowed domains:', window.uiActionAllowedDomains);
    addToLog('notification', `UI Actions Handler initialized with allowed domains: ${window.uiActionAllowedDomains.join(', ')}`);
    
    // Helper functions for UI feedback
    function showLoading(message) {
        // Create or update loading indicator
        let loadingDiv = document.getElementById('ui-action-loading');
        if (!loadingDiv) {
            loadingDiv = document.createElement('div');
            loadingDiv.id = 'ui-action-loading';
            loadingDiv.className = 'ui-action-loading';
            loadingDiv.innerHTML = `
                <div class="ui-action-loading-content">
                    <div class="ui-action-spinner"></div>
                    <div class="ui-action-loading-message">${message}</div>
                </div>
            `;
            document.body.appendChild(loadingDiv);
        } else {
            loadingDiv.querySelector('.ui-action-loading-message').textContent = message;
            loadingDiv.style.display = 'block';
        }
        
        // Add CSS if not already present
        if (!document.getElementById('ui-action-styles')) {
            const style = document.createElement('style');
            style.id = 'ui-action-styles';
            style.textContent = `
                .ui-action-loading {
                    position: fixed;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    background-color: rgba(0, 0, 0, 0.5);
                    z-index: 9999;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                }
                
                .ui-action-loading-content {
                    background-color: white;
                    padding: 20px;
                    border-radius: 8px;
                    text-align: center;
                    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
                }
                
                .ui-action-spinner {
                    border: 4px solid #f3f3f3;
                    border-top: 4px solid #3498db;
                    border-radius: 50%;
                    width: 30px;
                    height: 30px;
                    animation: ui-action-spin 1s linear infinite;
                    margin: 0 auto 10px;
                }
                
                @keyframes ui-action-spin {
                    0% { transform: rotate(0deg); }
                    100% { transform: rotate(360deg); }
                }
                
                .ui-action-loading-message {
                    margin-top: 10px;
                    font-weight: bold;
                }
                
                .ui-action-result {
                    position: fixed;
                    top: 20px;
                    right: 20px;
                    max-width: 400px;
                    background-color: white;
                    border: 1px solid #ddd;
                    border-radius: 8px;
                    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
                    z-index: 10000;
                    max-height: 80vh;
                    overflow-y: auto;
                }
                
                .ui-action-result-header {
                    padding: 10px;
                    background-color: #f8f9fa;
                    border-bottom: 1px solid #ddd;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                }
                
                .ui-action-result-content {
                    padding: 10px;
                    font-family: monospace;
                    white-space: pre-wrap;
                }
                
                .ui-action-result-close {
                    background: none;
                    border: none;
                    font-size: 18px;
                    cursor: pointer;
                    padding: 0;
                    width: 24px;
                    height: 24px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                }
                
                .ui-action-error {
                    position: fixed;
                    top: 20px;
                    right: 20px;
                    max-width: 400px;
                    background-color: #f8d7da;
                    border: 1px solid #f5c6cb;
                    border-radius: 8px;
                    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
                    z-index: 10000;
                    color: #721c24;
                }
                
                .ui-action-error-header {
                    padding: 10px;
                    background-color: #f1b0b7;
                    border-bottom: 1px solid #f5c6cb;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                }
                
                .ui-action-toast-container {
                    position: fixed;
                    top: 20px;
                    right: 20px;
                    z-index: 10001;
                    display: flex;
                    flex-direction: column;
                    gap: 10px;
                }
                
                .ui-action-toast {
                    background-color: #d1ecf1;
                    border: 1px solid #bee5eb;
                    border-radius: 8px;
                    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
                    max-width: 400px;
                }
                
                .ui-action-toast-header {
                    padding: 10px;
                    background-color: #b8daff;
                    border-bottom: 1px solid #bee5eb;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    border-radius: 8px 8px 0 0;
                }
                
                .ui-action-toast-content {
                    padding: 10px;
                }
                
                .ui-action-log-container {
                    position: fixed;
                    bottom: 0;
                    left: 0;
                    right: 0;
                    background-color: #f8f9fa;
                    border-top: 1px solid #ddd;
                    box-shadow: 0 -2px 4px rgba(0, 0, 0, 0.1);
                    z-index: 9998;
                    max-height: 300px;
                    display: flex;
                    flex-direction: column;
                }
                
                .ui-action-log-header {
                    padding: 10px;
                    background-color: #e9ecef;
                    border-bottom: 1px solid #ddd;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                }
                
                .ui-action-log-button {
                    background-color: #007bff;
                    color: white;
                    border: none;
                    border-radius: 4px;
                    padding: 5px 10px;
                    margin-left: 5px;
                    cursor: pointer;
                }
                
                .ui-action-log-button:hover {
                    background-color: #0056b3;
                }
                
                .ui-action-log {
                    flex: 1;
                    overflow-y: auto;
                    padding: 10px;
                    font-family: monospace;
                    font-size: 14px;
                    background-color: #fff;
                    max-height: 250px;
                }
                
                .ui-action-log-entry {
                    margin-bottom: 5px;
                    padding: 3px 0;
                    border-bottom: 1px solid #f0f0f0;
                }
                
                .ui-action-log-timestamp {
                    color: #6c757d;
                    font-weight: bold;
                }
                
                .ui-action-log-message {
                    color: #212529;
                }
                
                .ui-action-log-notification {
                    color: #0c5460;
                }
                
                .ui-action-log-error {
                    color: #721c24;
                    background-color: #f8d7da;
                }
                
                .ui-action-log-tool {
                    color: #004085;
                    background-color: #cce5ff;
                }
                
                .remote-dom-container {
                    border: 1px solid #ddd;
                    border-radius: 4px;
                    padding: 15px;
                    background-color: #f8f9fa;
                    margin-bottom: 15px;
                }
                
                .remote-dom-components {
                    margin-top: 10px;
                }
                
                .remote-dom-component {
                    border: 1px solid #ccc;
                    border-radius: 4px;
                    padding: 10px;
                    margin-bottom: 10px;
                    background-color: white;
                }
                
                .remote-dom-component strong {
                    color: #007bff;
                }
                
                .component-props {
                    margin: 5px 0;
                    padding-left: 15px;
                }
                
                .component-props li {
                    margin: 2px 0;
                }
            `;
            document.head.appendChild(style);
        }
    }
    
    function hideLoading() {
        const loadingDiv = document.getElementById('ui-action-loading');
        if (loadingDiv) {
            loadingDiv.style.display = 'none';
        }
    }
    
    function showResult(message, data) {
        // Hide loading indicator
        hideLoading();
        
        // Add to log console
        addToLog('tool', `${message}: ${JSON.stringify(data, null, 2)}`);
        
        // Create result display
        const resultDiv = document.createElement('div');
        resultDiv.className = 'ui-action-result';
        resultDiv.innerHTML = `
            <div class="ui-action-result-header">
                <strong>Result</strong>
                <button class="ui-action-result-close" onclick="this.closest('.ui-action-result').remove()">&times;</button>
            </div>
            <div class="ui-action-result-content">${message}\n\n${JSON.stringify(data, null, 2)}</div>
        `;
        
        document.body.appendChild(resultDiv);
        
        // Auto-remove after 10 seconds
        setTimeout(() => {
            if (resultDiv.parentNode) {
                resultDiv.remove();
            }
        }, 10000);
    }
    
    function showError(message) {
        // Hide loading indicator
        hideLoading();
        
        // Add to log console
        addToLog('error', message);
        
        // Create error display
        const errorDiv = document.createElement('div');
        errorDiv.className = 'ui-action-error';
        errorDiv.innerHTML = `
            <div class="ui-action-error-header">
                <strong>Error</strong>
                <button class="ui-action-result-close" onclick="this.closest('.ui-action-error').remove()">&times;</button>
            </div>
            <div class="ui-action-result-content">${message}</div>
        `;
        
        document.body.appendChild(errorDiv);
        
        // Auto-remove after 10 seconds
        setTimeout(() => {
            if (errorDiv.parentNode) {
                errorDiv.remove();
            }
        }, 10000);
    }
    
    function showNotification(message) {
        // Add to log console
        addToLog('notification', message);
        
        // Create toast notification display
        const toastDiv = document.createElement('div');
        toastDiv.className = 'ui-action-toast';
        toastDiv.innerHTML = `
            <div class="ui-action-toast-header">
                <strong>Notification</strong>
                <button class="ui-action-result-close" onclick="this.closest('.ui-action-toast').remove()">&times;</button>
            </div>
            <div class="ui-action-toast-content">${message}</div>
        `;
        
        // Add to toast container
        let toastContainer = document.getElementById('ui-action-toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'ui-action-toast-container';
            toastContainer.className = 'ui-action-toast-container';
            document.body.appendChild(toastContainer);
        }
        toastContainer.appendChild(toastDiv);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (toastDiv.parentNode) {
                toastDiv.remove();
                // Remove container if empty
                if (toastContainer.children.length === 0) {
                    toastContainer.remove();
                }
            }
        }, 5000);
    }
    
    // Add message to log console
    function addToLog(type, message) {
        // Create or get log container
        let logContainer = document.getElementById('ui-action-log');
        if (!logContainer) {
            createLogConsole();
            logContainer = document.getElementById('ui-action-log');
        }
        
        // Create log entry
        const timestamp = new Date().toLocaleTimeString();
        const logEntry = document.createElement('div');
        logEntry.className = `ui-action-log-entry ui-action-log-${type}`;
        logEntry.innerHTML = `<span class="ui-action-log-timestamp">[${timestamp}]</span> <span class="ui-action-log-message">${message}</span>`;
        
        // Add to log
        logContainer.appendChild(logEntry);
        
        // Scroll to bottom
        logContainer.scrollTop = logContainer.scrollHeight;
    }
    
    // Create log console
    function createLogConsole() {
        // Check if log console already exists
        if (document.getElementById('ui-action-log-container')) {
            return;
        }
        
        // Create log container
        const logContainer = document.createElement('div');
        logContainer.id = 'ui-action-log-container';
        logContainer.className = 'ui-action-log-container';
        logContainer.innerHTML = `
            <div class="ui-action-log-header">
                <strong>Console Log</strong>
                <div>
                    <button id="ui-action-log-clear" class="ui-action-log-button">Clear</button>
                    <button id="ui-action-log-toggle" class="ui-action-log-button">Hide</button>
                </div>
            </div>
            <div id="ui-action-log" class="ui-action-log"></div>
        `;
        
        document.body.appendChild(logContainer);
        
        // Add event listeners
        document.getElementById('ui-action-log-clear').addEventListener('click', function() {
            const log = document.getElementById('ui-action-log');
            if (log) {
                log.innerHTML = '';
            }
        });
        
        document.getElementById('ui-action-log-toggle').addEventListener('click', function() {
            const log = document.getElementById('ui-action-log');
            const button = this;
            if (log.style.display === 'none') {
                log.style.display = 'block';
                button.textContent = 'Hide';
            } else {
                log.style.display = 'none';
                button.textContent = 'Show';
            }
        });
    }
    
    // Initialize log console on page load
    document.addEventListener('DOMContentLoaded', function() {
        createLogConsole();
    });
    
    // Initialize Remote DOM widget
    window.initializeRemoteDomWidget = function(resourceUri) {
        console.log('Initializing Remote DOM widget for resource:', resourceUri);
        addToLog('notification', `Initializing Remote DOM widget: ${resourceUri}`);
        
        // In a real implementation, this would initialize the Remote DOM widget
        // and set up event listeners for component interactions
        
        // Add click event listeners to Remote DOM components
        const container = document.getElementById(`remote-dom-container-${resourceUri}`);
        if (container) {
            container.addEventListener('click', function(event) {
                // Handle component interactions
                const componentElement = event.target.closest('.remote-dom-component');
                if (componentElement) {
                    const componentId = componentElement.dataset.componentId;
                    const componentType = componentElement.dataset.componentType;
                    
                    console.log('Remote DOM component clicked:', componentId, componentType);
                    addToLog('notification', `Remote DOM component clicked: ${componentType} (${componentId})`);
                    
                    // Dispatch Remote DOM action
                    dispatchRemoteDomAction(resourceUri, componentId, componentType, 'click');
                }
            });
        }
    };
    
    // Dispatch Remote DOM action to backend
    window.dispatchRemoteDomAction = function(resourceUri, componentId, componentType, actionType, payload) {
        console.log('Dispatching Remote DOM action:', actionType, componentId, componentType);
        addToLog('tool', `Dispatching Remote DOM action: ${actionType} on ${componentType} (${componentId})`);
        
        // Show loading state
        showLoading(`Processing Remote DOM action: ${actionType}`);
        
        // Prepare the request
        const requestData = {
            type: 'remote-dom',
            payload: {
                resourceUri: resourceUri,
                componentId: componentId,
                componentType: componentType,
                actionType: actionType,
                payload: payload || {}
            },
            sessionId: window.currentSessionId || getQueryParam('session_id') || ''
        };
        
        // Call the backend UI action endpoint
        fetch('/mcp/ui-action', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(requestData)
        })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            console.log('Remote DOM action result:', data);
            hideLoading();
            
            // Show result to user
            showResult(`Remote DOM action '${actionType}' executed successfully`, data);
            
            // Handle any UI updates based on the result
            handleRemoteDomActionResult(data);
        })
        .catch(error => {
            console.error('Remote DOM action failed:', error);
            hideLoading();
            showError(`Remote DOM action '${actionType}' failed: ${error.message}`);
        });
    };
    
    // Handle Remote DOM action result
    window.handleRemoteDomActionResult = function(result) {
        // In a real implementation, this would update the UI based on the action result
        console.log('Handling Remote DOM action result:', result);
        
        // Check if there are any UI updates in the result
        if (result && result.uiUpdates) {
            // Apply UI updates
            applyUiUpdates(result.uiUpdates);
        }
    };
    
    // Apply UI updates from Remote DOM action result
    window.applyUiUpdates = function(updates) {
        // In a real implementation, this would apply UI updates to the Remote DOM components
        console.log('Applying UI updates:', updates);
        addToLog('notification', 'Applying UI updates from Remote DOM action');
    };
    
    // Add a global function to handle Remote DOM messages from iframes
    window.handleRemoteDomMessage = function(event) {
        try {
            const data = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
            
            if (data && data.type === 'remoteDomAction') {
                console.log('Remote DOM action received from iframe:', data);
                addToLog('notification', `Remote DOM action received: ${data.action}`);
                
                // Dispatch the action
                if (data.payload) {
                    dispatchRemoteDomAction(
                        data.payload.resourceUri,
                        data.payload.componentId,
                        data.payload.componentType,
                        data.payload.action,
                        data.payload.payload
                    );
                }
            }
        } catch (e) {
            console.error('Error processing Remote DOM message:', e);
        }
    };
    
    // Helper function to get query parameters
    function getQueryParam(name) {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(name);
    }
    
    // Listen for messages from iframes
    window.addEventListener('message', function(event) {
        // Security check - only accept messages from same origin or trusted sources
        // In a production environment, you should implement proper origin checking
        
        try {
            const data = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
            
            if (data && data.type === 'onUIAction') {
                handleUIAction(data.payload);
            } else if (data && data.type === 'remoteDomAction') {
                // Handle Remote DOM messages
                handleRemoteDomMessage(event);
            }
        } catch (e) {
            console.error('Error processing message:', e);
        }
    });
    
    console.log('UI Actions Handler initialized');
})();
