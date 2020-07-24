# Hack For Manufacturing Data Integration

A Repository for Manufacturing Data Integration

## OPC-UA Information model of Manufacturing in a Box (MiaB)

The MiaB OPC-UA Information model was exported from the OPC-UA Server as nodes set file. It's stored in the folder 'assets' and has the name [miab10-nodeset2.xml](./assets/miab10-nodeset2.xml)

All MiaB related data points are contained in one folder object with variables (see node with id `ns=2;s=Beijer.nsuri=TagProvider;s=Tags`):

```xml
  <UAObject NodeId="ns=2;s=Beijer.nsuri=TagProvider;s=Tags" BrowseName="2:Tags" ParentNodeId="ns=2;s=Beijer">
    <DisplayName>Tags</DisplayName>
    <References>
      <Reference ReferenceType="HasTypeDefinition">i=61</Reference>
      <Reference ReferenceType="HasComponent">ns=2;s=Beijer.nsuri=TagProvider;s=Tag_D1_Analog</Reference>
      <Reference ReferenceType="HasComponent">ns=2;s=Beijer.nsuri=TagProvider;s=Tag_Modbus_Temperature</Reference>
      <Reference ReferenceType="HasComponent">ns=2;s=Beijer.nsuri=TagProvider;s=Tag_Modbus_CO2</Reference>
      <Reference ReferenceType="HasComponent">ns=2;s=Beijer.nsuri=TagProvider;s=Tag_D0_Digital</Reference>
      <Reference ReferenceType="HasComponent">ns=2;s=Beijer.nsuri=TagProvider;s=Tag_Modbus_Humidity</Reference>
      <Reference ReferenceType="HasComponent">ns=2;s=Beijer.nsuri=TagProvider;s=Tag_D2_Blue</Reference>
      <Reference ReferenceType="Organizes" IsForward="false">ns=2;s=Beijer</Reference>
    </References>
  </UAObject>
    <UAVariable NodeId="ns=2;s=Beijer.nsuri=TagProvider;s=Tag_D1_Analog" BrowseName="2:Tag_D1_Analog" ParentNodeId="ns=2;s=Beijer.nsuri=TagProvider;s=Tags" DataType="Int16" AccessLevel="3" UserAccessLevel="3">
    <DisplayName>Tag_D1_Analog</DisplayName>
    <References>
      <Reference ReferenceType="HasTypeDefinition">i=63</Reference>
      <Reference ReferenceType="HasComponent" IsForward="false">ns=2;s=Beijer.nsuri=TagProvider;s=Tags</Reference>
    </References>
    <Value>
      <uax:Int16>187</uax:Int16>
    </Value>
  </UAVariable>
  ...
```

The full data points (6) list looks like:

| Node ID | Displayname | Type |
| ----------- | ----------- |----------- |
| ns=2;s=Beijer.nsuri=TagProvider;s=Tag_D1_Analog | Tag_D1_Analog | 16 bit signed integer |
| ns=2;s=Beijer.nsuri=TagProvider;s=Tag_Modbus_Temperature | Tag_Modbus_Temperature | 16 bit signed integer |
| ns=2;s=Beijer.nsuri=TagProvider;s=Tag_Modbus_CO2 | Tag_Modbus_CO2 | 16 bit signed integer |
| ns=2;s=Beijer.nsuri=TagProvider;s=Tag_D0_Digital | Tag_D0_Digital | 16 bit signed integer |
| ns=2;s=Beijer.nsuri=TagProvider;s=Tag_Modbus_Humidity | Tag_Modbus_Humidity | 16 bit signed integer |
| ns=2;s=Beijer.nsuri=TagProvider;s=Tag_D2_Blue | Tag_D2_Blue | 16 bit signed integer |

## OPC-UA Subscriptions (Published nodes of OPC Publisher)

```json
[
    {
        "EndpointUrl": "opc.tcp://192.168.4.123:4841/Softing_dataFEED_OPC_Suite_Configuration1",
        "UseSecurity": false,
        "OpcNodes": [
            {
                "Id": "nsu=Beijer;s=Beijer.nsuri=TagProvider;s=Tag_D1_Analog"
            },
            {
                "Id": "nsu=Beijer;s=Beijer.nsuri=TagProvider;s=Tag_D0_Digital"
            },
            {
                "Id": "nsu=Beijer;s=Beijer.nsuri=TagProvider;s=Tag_Modbus_Temperature"
            },
            {
                "Id": "nsu=Beijer;s=Beijer.nsuri=TagProvider;s=Tag_Modbus_CO2"
            },
            {
                "Id": "nsu=Beijer;s=Beijer.nsuri=TagProvider;s=Tag_Modbus_Humidity"
            },
            {
                "Id": "nsu=Beijer;s=Beijer.nsuri=TagProvider;s=Tag_D2_Blue"
            }
        ]
    }
]
```
