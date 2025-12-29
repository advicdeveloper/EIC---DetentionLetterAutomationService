# Detention Letter Dynamics 365 Plugin

## Overview

This plugin converts the Windows Service timer-based detention letter processing to a Dynamics 365 Online plugin architecture.

## Plugin Classes

### 1. PluginBase.cs
Base class that provides common functionality for all plugins including:
- Error handling
- Tracing
- Service provider management
- Organization service initialization

### 2. DetentionLetterPlugin.cs
Main plugin that processes detention letters. Converts the logic from `_aTimer_Elapsed` method in the Windows Service to a Dynamics 365 plugin.

## Registration Options

### Option 1: Scheduled Batch Processing (Recommended)
Register this plugin to run on a schedule similar to the original Windows Service:

1. **Create a Custom Action** in Dynamics 365:
   - Name: `new_ProcessDetentionLetters`
   - No input/output parameters needed
   - This allows you to trigger the plugin manually or via workflow

2. **Register the Plugin**:
   - **Message**: `new_ProcessDetentionLetters` (or your custom action name)
   - **Stage**: Post-operation
   - **Execution Mode**: Asynchronous
   - **Entity**: none (custom action)

3. **Schedule Execution** using one of these methods:
   - **Power Automate (Cloud Flow)**: Create a scheduled cloud flow that triggers the custom action
   - **Recurring Workflow**: Create a workflow that runs periodically and calls the custom action
   - **Azure Logic Apps**: Use Azure Logic Apps to call the Dynamics 365 API on a schedule

### Option 2: Event-Driven Processing
Register on Sales Order events to process immediately when conditions are met:

1. **Register the Plugin**:
   - **Entity**: salesorder
   - **Message**: Update (or Create)
   - **Stage**: Post-operation
   - **Execution Mode**: Asynchronous
   - **Filtering Attributes**: (relevant fields that trigger detention letter processing)

2. **Add Pre-Filtering**:
   - Modify the plugin to check if the order meets detention letter criteria before processing
   - This prevents unnecessary executions

## Plugin Registration Steps

### Prerequisites
- Plugin Registration Tool (part of Dynamics 365 SDK)
- Compiled plugin assembly (.dll)
- Dynamics 365 Online environment

### Registration Process

1. **Build the Plugin**:
   ```bash
   # Build in Release mode
   dotnet build --configuration Release
   # Or using Visual Studio: Build > Build Solution
   ```

2. **Sign the Assembly** (Required for Dynamics 365):
   - Right-click project > Properties > Signing
   - Check "Sign the assembly"
   - Create new strong name key file

3. **Register Using Plugin Registration Tool**:
   ```
   a. Connect to your Dynamics 365 organization
   b. Click "Register" > "Register New Assembly"
   c. Browse to your compiled .dll file
   d. Select isolation mode: "Sandbox"
   e. Select location: "Database"
   f. Click "Register Selected Plugins"
   ```

4. **Register Step**:
   ```
   a. Right-click on DetentionLetterPlugin
   b. Click "Register New Step"
   c. Configure:
      - Message: (your choice based on option above)
      - Primary Entity: (salesorder or none for custom action)
      - Event Pipeline Stage: Post-operation
      - Execution Mode: Asynchronous
      - Execution Order: 1
   d. Click "Register New Step"
   ```

## Scheduled Execution Example (Power Automate)

Create a Cloud Flow in Power Automate:

1. **Trigger**: Recurrence
   - Interval: 30 (or as per your requirement)
   - Frequency: Minute
   - Time zone: Your local time zone

2. **Action**: Perform an unbound action
   - Environment: (Your Dynamics 365 environment)
   - Action Name: new_ProcessDetentionLetters

## Configuration

### App Settings
The plugin will use the same configuration as the Windows Service. Ensure these are available in:
- **Dynamics 365 Environment Variables**
- **Unsecure/Secure Configuration** in plugin step registration

Example configurations to migrate:
- `TimerValue` → Convert to Power Automate recurrence interval
- Email settings
- Report paths
- Any other app settings from App.config

### Secure Configuration
For sensitive data like connection strings or API keys:
1. Use Secure Configuration in plugin step registration
2. Access via constructor parameter:
```csharp
public DetentionLetterPlugin(string unsecureConfig, string secureConfig)
    : base(typeof(DetentionLetterPlugin))
{
    // Parse and use configurations
}
```

## Migration from Windows Service

### Key Differences

| Windows Service | Dynamics 365 Plugin |
|----------------|---------------------|
| Timer-based execution | Event-based or scheduled |
| Runs continuously | Runs on-demand |
| App.config for settings | Environment variables or config |
| Local file system access | Azure Blob Storage recommended |
| Synchronous processing | Async recommended |

### Code Changes Made

1. **Removed**:
   - Timer initialization and management
   - ServiceBase inheritance
   - Start/Stop methods

2. **Added**:
   - IPlugin implementation
   - Service provider handling
   - Tracing service integration
   - Error handling for plugin context

3. **Converted**:
   - `_aTimer_Elapsed` → `ProcessDetentionLetters` method
   - Timer start/stop → Plugin lifecycle management
   - App settings → Plugin configuration

## Deployment Checklist

- [ ] Build plugin in Release mode
- [ ] Sign assembly with strong name key
- [ ] Test in Dynamics 365 Sandbox environment
- [ ] Register plugin assembly
- [ ] Register plugin step
- [ ] Create custom action (if using scheduled approach)
- [ ] Set up Power Automate flow or workflow
- [ ] Configure environment variables
- [ ] Test execution
- [ ] Monitor plugin trace logs
- [ ] Deploy to Production

## Monitoring and Debugging

### View Plugin Trace Logs
1. Navigate to **Settings** > **Customizations** > **Plug-in Trace Log**
2. Filter by plugin name: `DetentionLetterPlugin`
3. Review execution details and any errors

### Enable Tracing
In Plugin Registration Tool:
1. Right-click on the step
2. Select "Enable/Disable Profiling"
3. Choose profiling options

### Common Issues

1. **Plugin timeout**: If processing many records, consider batch size limits
2. **Depth violations**: Check recursion prevention logic
3. **File access**: Migrate from local file system to Azure Blob Storage
4. **Email sending**: Ensure proper SMTP configuration or use Dynamics 365 email

## Performance Considerations

- Use **Asynchronous** execution mode for long-running operations
- Implement **batch size limits** if processing many records
- Use **early-bound** classes for better performance (optional)
- Consider **parallel processing** for multiple orders (with caution)

## Support

For issues or questions:
1. Check Plugin Trace Logs
2. Review Logger output
3. Verify plugin registration configuration
4. Test in Sandbox environment first
