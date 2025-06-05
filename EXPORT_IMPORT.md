# Export/Import Classification Data

This document describes the new Export/Import functionality for classification data in the Classify Data application.

## Overview

The Export/Import functionality allows you to:
- Export all column classification data from a database to a structured JSON file
- Import classification data from a JSON file to apply classifications to database columns
- Test the export functionality to validate data retrieval

## Features

### Export Data
- **Action**: ExportData
- **Location**: Available on Database entities (ShowedOn: 5)
- **Function**: Exports all column classification data to a timestamped JSON file
- **Output Format**: Structured JSON containing:
  - Export metadata (timestamp, database name)
  - Array of column classifications with schema, table, column, type, information type, sensitivity label, and description

### Import Data  
- **Action**: ImportData
- **Location**: Available on Database entities (ShowedOn: 5)
- **Function**: Imports column classification data from JSON format
- **Input**: JSON data matching the export format
- **Validation**: Checks for required fields and existing columns
- **Error Handling**: Reports success/failure counts and detailed error messages

### Test Export
- **Action**: TestExportData
- **Location**: Available on Database entities (ShowedOn: 5)
- **Function**: Tests the export functionality without creating a file
- **Purpose**: Validates database connectivity and column discovery

## Usage

### Exporting Classification Data
1. Navigate to a Database entity in the application
2. Click the "Export Data" action
3. The system will generate a JSON file with all classification data
4. File name format: `ClassifyData_Export_{DatabaseName}_{Timestamp}.json`

### Importing Classification Data
1. Prepare a JSON file using the export format
2. Navigate to the target Database entity
3. Click the "Import Data" action
4. Provide the JSON data (implementation supports multiple input methods)
5. Review the import results and any error messages

## JSON Format

The export/import uses the following JSON structure:

```json
{
  "ExportedAt": "2024-01-01T12:00:00Z",
  "DatabaseName": "YourDatabase",
  "Columns": [
    {
      "Schema": "dbo",
      "Table": "Users",
      "Column": "Email",
      "Type": "varchar",
      "InformationTypeId": "5C503E21-22C6-81FA-620B-F369B8EC38D1",
      "InformationTypeName": "Contact Info",
      "SensitivityLabelId": "684a0db2-d514-49d8-8c0c-df84a7b083eb",
      "SensitivityLabelName": "General",
      "Description": "User email address"
    }
  ]
}
```

## Implementation Details

### Technical Components
- **ClassificationData**: Main DTO for export/import operations
- **ColumnClassification**: DTO representing individual column classification
- **DatabaseActions**: Contains ExportData, ImportData, and TestExportData methods
- **Error Handling**: Comprehensive validation and error reporting
- **SQL Security**: Protected against SQL injection using parameterized queries

### Database Integration
- Reads from and writes to SQL Server extended properties
- Supports all standard information types and sensitivity labels
- Preserves existing data integrity during import operations
- Only applies non-empty classification values

### Safety Features
- Validates column existence before applying classifications
- Skips columns with empty/null classification values
- Provides detailed error reporting for failed operations
- Maintains transaction consistency

## Future Enhancements

The current implementation provides a solid foundation for export/import functionality. Potential enhancements could include:
- CSV export/import format support
- Bulk classification templates
- Classification rule validation
- Advanced filtering and selection options
- Integration with external classification systems