# How to create AppServer application #
----------------

Create empty C# class library project, install app server sdk nuget package 

```
  Install-Package Inceptum.AppServer.Sdk
``` 

This will download required nuget packages and add references to assemblies

Application **MUST** 
	
1. have one class which implements IHostedApplication	 
2. have its assembly marked with HostedApplication attribute 
3. have nuspec file with tag Inceptum.AppServer.Application

Application **CAN** 

1. declare any number of public classes which implement IWindsorInstaller, all of them will be installed into container on application start;

# Code templates #
----------------
To achive all above please create following files under project root folder:

1. Applicaton.cs
2. $ProjectName$.nuspec - make sure that the name of nuspec is the same as the name of the csproj
3. Windsor\ApplicationInstaller.cs
4. Properties\ApplicationInfo.cs 

## Application.cs  ##
    
    using System;
    using Castle.Core.Logging;
    using Inceptum.AppServer;
    
    namespace YOUR.NAMESPACE
    {
    	class HostedApplication : IHostedApplication
    	{
    		private readonly ILogger m_Logger;
    				
    		public HostedApplication(ILogger logger) 
    		{
    			/* NOTE: logger and other application dependencies are resolved from container
    			install components you need via Windsor\ApplicationInstaller.cs installer */
    
    			if (logger == null) throw new ArgumentNullException("logger");
    			m_Logger = logger;
    		}	
    
    		public void Start()
    		{
    			/*TODO: implement application start logic here, or leave it empty and 
    			create components using IStartable interface from StartableFacility*/
    
    			m_Logger.Debug("Start method of {0} was called" + GetType().Name);
    
    			throw new NotImplementedException();
    		}
    	}
    }

## Application.nuspec ##

    <?xml version="1.0"?>
    <package >
      <metadata>
    	<id>YOUR.PACKAGE.ID</id>
    	<version>$version$</version>
    	<title>YOUR.PACKAGE.ID</title>
    	<authors>TODO set authors</authors>
    	<owners>TODO set owners</owners>
    	<requireLicenseAcceptance>false</requireLicenseAcceptance>
    	<description>TODO describe application</description>
    	<releaseNotes></releaseNotes>
    	<copyright>Copyright 2013</copyright>
    	<tags>Inceptum.AppServer.Application</tags>
      </metadata>
    </package>

## Windsor\ApplicationInstaller.cs ##

    using System;
    using Castle.Facilities.Startable;
    using Castle.Facilities.TypedFactory;
    using Castle.MicroKernel.Registration;
    using Castle.MicroKernel.Resolvers.SpecializedResolvers;
    using Castle.MicroKernel.SubSystems.Configuration;
    using Castle.Windsor;
    using Inceptum.AppServer.Configuration;
    
    namespace YOUR.NAMESPACE.Windsor
    {
    	public class HostedApplicationInstaller : IWindsorInstaller
    	{
    		public void Install(IWindsorContainer container, IConfigurationStore store)
		    {
			    /*Add default components commonly used, you can remove registrations if you don't need it*/
			    container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));
			    container.AddFacility<StartableFacility>(f => {});
			    container.AddFacility<TypedFactoryFacility>();
			    
			     
			    /*TODO: you can configure transports like this
			    container.AddFacility<ConfigurationFacility>(f => f.Configuration("configuration")
			       .ConfigureTransports("transports", "{environment}", "{machineName}")
			       .ConfigureConnectionStrings("transports", "{environment}", "{machineName}"));
			    */
			    
			    /*TODO: configure and install required components into windsor container  */ 
		    }
		}
    }


## Properties\ApplicationInfo.cs ##
    using Inceptum.AppServer;
    
    [assembly: HostedApplication("YOUR.PACKAGE.ID")]





