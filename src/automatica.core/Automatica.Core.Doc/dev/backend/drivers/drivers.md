# Automatica.Core.Driver
## How to
The DriverFactory is your entry point to Automtaica.Core. See the [API](/api/Automatica.Core.Driver.DriverFactory.html).

Here is a simple example on how to create a driver using the `Automatica.Core.Constants`.

### DriverFactory

```C#
public static readonly Guid InterfaceId = new Guid("5926638a-e9f8-48cb-8401-c8042170ff1b");

public static readonly Guid BusId = new Guid("2ba2fdfe-3df0-4986-80c1-d0f695d64fdc");
public static readonly Guid ValueId = new Guid("d46b8d4d-29e6-45bd-ba62-9463692bcbd7");

public static readonly Guid PiId = new Guid("36a0da8a-2735-4f83-91ef-9af90262de32");
public static readonly Guid HalfPiId = new Guid("bde14ed8-24b3-476b-9c8a-751da617a50b");
public static readonly Guid DoublePiId = new Guid("82e579a7-935e-463b-9d26-c75b31113553");

/// <summary>
/// The entry point to set your driver definition
/// </summary>
/// <param name="factory">Interface to the database to generate your templates</param>
public override void InitNodeTemplates(INodeTemplateFactory factory)
{
    // first of all create a interface type for your subnodes
     factory.CreateInterfaceType(InterfaceId, "CONSTANTS.NAME", "CONSTANTS.DESCRIPTION", int.MaxValue, 1, true);

    // the root node that can be created under the virtual node
    factory.CreateNodeTemplate(BusId, "CONSTANTS.NAME", "CONSTANTS.DESCRIPTION", "consts", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual),
        InterfaceId, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

    // create a generic constants node - the value of the constants is going to be defined as property
    factory.CreateNodeTemplate(ValueId, "CONSTANTS.NODE.CONSTANT.NAME", "CONSTANTS.NODE.CONSTANT.DESCRIPTION", "const", InterfaceId,
        GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Integer, Int32.MaxValue, false);
    
    // create the property for the constants node - where the user can enter the consant value
    factory.CreatePropertyTemplate(PropertyValueId, "CONSTANTS.PROPERTY.VALUE.NAME", "CONSTANTS.PROPERTY.VALUE.DESCRIPTION", "const_value", PropertyTemplateType.Integer,
        ValueId, "CONSTANTS.CATEGORY.VALUE", true, false, "", "", 1, 1);

    // create some predefined constants 
    // PI
    factory.CreateNodeTemplate(PiId, "CONSTANTS.NODE.PI.NAME", "CONSTANTS.NODE.PI.DESCRIPTION", "const_pi", InterfaceId,
        GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Double, Int32.MaxValue, false);

    // PI / 2
    factory.CreateNodeTemplate(HalfPiId, "CONSTANTS.NODE.HALF_PI.NAME", "CONSTANTS.NODE.HALF_PI.DESCRIPTION", "const_halfpi", InterfaceId,
        GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Double, Int32.MaxValue, false);

    // PI * 2
    factory.CreateNodeTemplate(DoublePiId, "CONSTANTS.NODE.DOUBLE_PI.NAME", "CONSTANTS.NODE.DOUBLE_PI.DESCRIPTION", "const_doublepi", InterfaceId,
        GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Double, Int32.MaxValue, false);
}
```
You can find more in the documentation [Automatica.Core.Documentation](https://developer.automaticacore.com/docu)


### How to implement the instances
Here you have the `public override IDriver CreateDriver(IDriverContext config)` method as your starting point. For every node in your NodeTemplate tree this method is called. 

```C#
public override IDriver CreateDriver(IDriverContext config)
{
    return new ConstantsDriver(config);
}
```

The `ConstantsDriver` is just a container for more nodes. So the code for the implementation is very raw.

```C#
public class ConstantsDriver : DriverBase
{ 
    
    public ConstantsDriver(IDriverContext ctx) : base(ctx)
    {
    }
    
    public override IDriverNode CreateDriverNode(IDriverContext ctx)
    {
        return new Constant(ctx);
    }
}
```

Here is the implementation for the `Constant`
```C#
public class Constant : DriverBase
{
    // the timer is used to sent the value every 10 sec
    private readonly Timer _dispatchTimer = new Timer();
    private double? _value = null;

    internal double? Value => this._value;

    public Constant(IDriverContext ctx) : base(ctx)
    {
        _dispatchTimer.Interval = 10000;
        _dispatchTimer.Elapsed += _dispatchTimer_Elapsed;
    }

    // timer elapsed method
    private void _dispatchTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (_value.HasValue)
        {
            // dispatch the value
            DispatchValue(_value);
        }
    }

    public override bool Init()
    {
        if (DriverContext.NodeInstance.This2NodeTemplateNavigation.Key == "const_pi")
        {
            _value = Math.PI;
        }
        else if (DriverContext.NodeInstance.This2NodeTemplateNavigation.Key == "const_halfpi")
        {
            _value = Math.PI / 2;
        }
        else if (DriverContext.NodeInstance.This2NodeTemplateNavigation.Key == "const_doublepi")
        {
            _value = Math.PI *2;
        }
        else
        {
            // user defined constat value
            var prop = GetPropertyValueInt("const_value"); // get the value property
            _value = Convert.ToDouble(prop);
        }
        return base.Init();
    }

    // only called once
    public override Task<bool> Start()
    {
        _dispatchTimer.Start();

        // initially dispatch the value on satrt
        DispatchValue(_value);

        return base.Start();
    }
    
    public override IDriverNode CreateDriverNode(IDriverContext ctx)
    {
        return null; //we have no more children, therefore return null
    }
}
```