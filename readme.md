# Our.Umbraco.SilentUpgrade

Silenty Upgrade Umbraco without showing anyone the install script.

1. Upgrade your umbraco files (via nuget or how ever you want)
2. Visit the site the upgrade happens - you don't see it.

## Usage : 

Has to be turned on in `Web.config` file

```
<add key="SilentUpgrade" value="true" />
```

The process will attempt to run through the installer steps as part
of the boot process. If this works then the users won't see anything
and the site will be updated. 

If the process fails then the site will fall back to the default install
screen and you will have to process through the steps as normal.

## Disclaimer
While every effort has been made to test the upgrade of umbraco between
versions using this package, we cannot accept any responsability to 
a botched update or missing things. **You should always test upgrades 
before putting them somewhere that you care about**

## Known Issues

- **Doesn't work for Umbraco 8.0.x versions**
 
  For reasons i've not yet worked out, the upgrade to a 8.0.x version 
  from another doesn't work (e.g 8.0.0 to 8.0.2). 

  Upgrading from 8.0.x works fine.
 

## Debuging

Should things not work as expected then you should see the standard
install screen. However if you want to debug the project then adding
the following line to your serilog.config file will enable debug 
logging of the process.

```
<add key="serilog:minimum-level:override:Our.Umbraco.SilentUpgrade" value="Debug"/>
```

Happy Upgrading.

