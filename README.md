
# Template Api Backend .NET 8
##  Descripción

  Template web api desarrollado en .Net 8 diseñado utilizando la arquitectura "Clean Arquitecture"  y configurado con las siguientes características fundamentales para su utilización en proyectos .Net.
  
---

##  Índice
- [Template Api Backend](#template-api-backend)
	- [Descripción](#descripción)
	- [Índice](#índice)
	- [Características y configuraciones principales](#características-y-configuraciones-principales)
	- [Tecnologías utilizadas](#tecnologías-utilizadas)
	- [Detalles](#detalles)
	- [Estructura de proyectos y directorios](#estructura-de-proyectos-y-directorios)
		- [Dependencias](#dependencias)
		- [Instalación](#instalación)
	- [Desarrollo y mantenimiento](#desarrollo-y-mantenimiento)
	- [Convenciones y estándares de desarrollo](#convenciones-y-estándares-de-desarrollo)
	- [Autores](#autores)
	- [Changelog](#changelog)
	- [[2.0.0] - 2024-09-04](#200---2024-09-04)
		- [Added](#added)

---

## Características y configuraciones principales
  
- Configuración de proveedor de identidad Auth0
- Manejo de excepciones global
- Configuración de Redis
- Configuración de Automapper
- Configuración de EntitiFramework 6 con contexto y comando para Scaffold, repositorio generico y metodos de extencion para paginado
- Configuración de Dapper
- Configuración estandar de Cors
- Configuración de Swagger con autenticación
- Configuración de Serilog
- Configuración de Application Insights
- Request Compression
- Controladores, servicios, factories y repositorios de demostración
- Estructura general de directorio
- Base de datos de la aplicación

---

## Tecnologías utilizadas
- .Net 6.0.4 LTS
- Entity Framework Core 6.0.4
- Dapper 2.1.35
- Serilog 6.0
- Automapper 12.0.1
- Auth0 Autentication Api 7.26
- StackExchange.Redis 2.5.61
- Swagger 6.6.2
- Polly 8.4.1
- Microsoft Application Insights 2.22.0
  
---

##  Detalles

*Clean Arquitecture*  

![enter image description here](https://miro.medium.com/max/1200/1*eRYOS-hWwJzByQ_gT34WkQ.png)

---
  
# Estructura de proyectos y directorios en .NET 8

## 1. Application.Core
**Descripción**: Este proyecto contiene la lógica de negocio principal de la aplicación. Es el "corazón" de la aplicación, de ahí el nombre "Core".

**Contenido típico**:
- **Entidades**: Clases que representan los objetos principales de dominio de la aplicación.
- **Interfaces**: Definiciones de contratos para los servicios, repositorios o cualquier otro tipo de dependencia externa.
- **Servicios**: Implementación de la lógica de negocio principal, como reglas o validaciones.
- **Casos de uso**: A veces este proyecto también incluye los "Use Cases" que implementan la lógica de cada acción en la aplicación.

**Independencia**: Este proyecto debería ser independiente de otros proyectos, como el de infraestructura o la interfaz de usuario.

---

## 2. Application.Infrastructure
**Descripción**: Aquí es donde se implementan los detalles concretos de la infraestructura, como el acceso a datos, integración con servicios externos, almacenamiento en la nube, etc. Este proyecto actúa como una "puerta" entre el Core y las dependencias externas.

**Contenido típico**:
- **Repositorios**: Implementación de los patrones de acceso a datos como el patrón **Repository**.
- **Servicios externos**: Implementaciones de interfaces para interactuar con APIs externas, mensajería, servicios de terceros, etc.
- **Configuración de base de datos**: Código de conexión, mapeo de entidades y migraciones de la base de datos.
- **Implementaciones de interfaces**: Todas las interfaces definidas en el Core son implementadas aquí.

**Dependencias**: Este proyecto depende del proyecto **Application.Core**, ya que necesita conocer las interfaces y entidades para implementarlas.

---

## 3. Application.Shared
**Descripción**: El propósito de este proyecto es contener elementos que pueden ser utilizados de manera compartida tanto por el Core como por otros proyectos dentro de la solución.

**Contenido típico**:
- **DTOs (Data Transfer Objects)**: Objetos utilizados para transferir datos entre las capas.
- **Utilidades**: Funciones, helpers o clases de utilidad que pueden ser comunes para varios proyectos.
- **Excepciones personalizadas**: Clases de manejo de excepciones que pueden ser lanzadas desde el Core y manejadas en otras capas.
- **Configuración compartida**: Elementos de configuración o constantes que son usados por múltiples capas.

---

## 4. Web.Api
**Descripción**: Este proyecto representa la capa de presentación o API de la aplicación. Es la puerta de entrada para los usuarios o clientes externos.

**Contenido típico**:
- **Controladores**: Clases que manejan las peticiones HTTP, coordinan la lógica de negocio llamando a los servicios del **Core** y devuelven las respuestas.
- **Configuración de rutas**: Definición de las rutas y endpoints de la API.
- **Manejo de autenticación y autorización**: Configuración y middleware para controlar el acceso de usuarios.
- **Servicios y Middlewares**: Los middlewares necesarios para manejar las peticiones, como logs, autenticación, compresión, etc.
- **Integración con Core**: Este proyecto utiliza el **Core** para ejecutar la lógica de negocio, así como también puede llamar al proyecto **Infrastructure** para interactuar con la base de datos.

---

## Resumen de la separación de responsabilidades:
- **Application.Core**: Lógica de negocio pura, sin dependencias externas.
- **Application.Infrastructure**: Implementación de las dependencias externas (acceso a datos, servicios externos).
- **Application.Shared**: Elementos compartidos entre las capas (DTOs, utilidades).
- **Web.Api**: Punto de entrada para los usuarios (controladores, endpoints de la API).
 

##  Dependencias

***Herramientas de desarrollo y dependencias***  

- [Visual Studio 2022 Professional](https://visualstudio.microsoft.com/es/thank-you-downloading-visual-studio/?sku=Professional&channel=Release&version=VS2022&source=VSLandingPage&cid=2030&passive=false)
- [.Net 8](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.8-windows-x64-installer)
- [Visual Studio Code](https://code.visualstudio.com/)
- [SQL Managment Studio](https://aka.ms/ssmsfullsetup)
- [Postman](https://dl.pstmn.io/download/latest/win64)
- [SQL Server 2022 Developer](https://www.microsoft.com/es-es/sql-server/sql-server-downloads)
  

***Extensiones***

*Visual Studio*

- [CodeMaid](https://marketplace.visualstudio.com/items?itemName=SteveCadwallader.CodeMaid)
  
---

###  Instalación  y uso

Generar una carpeta con el fin de alojar el repositorio, dicha carpeta se debe nombrar "Desarrollo" y debe estar ubicada en C:

Una vez clonado el repositorio se deberán de instalar las herramientas y dependencias listadas en la sección anterior.

El paso siguiente será realizar la restauración de la base de datos de la aplicación. Existe un respaldo que esta ubicado en \@Doc\Dbl\ApiUsersDataBase.bacpac. este debe ser restaurado utilizando SQL Managment Studio con la opción "Import Data-Tier Aplication".
Esta base de datos contiene una entidad de demostración únicamente. 

## Convenciones y estándares de desarrollo

- Clases de negocio:
	- Ubicación: Application.Core/Entities
	- Nomenclatura: PascalCase en singular
- Interfaces:
	- Ubicación: `Application.Core/Interfaces/{TipoDeInterfaz}`
	- Nomenclatura: PascalCase en singular, deben comenzar con "I" y terminar con el tipo de interfaz adecuado, Ej: `IPersonaService.cs` 
- ValueObjects
	- Ubicación: `Application.Core/ValueObject/{Ambito ValueObject}`
	- Nomenclatura: PascalCase en singular, Terminando con el sufijo `VO` Ej: `PersonaVO`
- Variables:
	 - Nomenclatura: CamelCase ,  Ej: `persona`
- Controladores: 
	- Clases destinadas a exponer los diferentes endpoints para el acceso a la aplicación. estos deberán de utilizar rutas y métodos http basados en el protocolo REST.
	- Ubicación: dentro de la carpeta controllers de cada api de dominio
	- Nomenclatura: 
			- PascalCase en singular
			- {Entidad} + sufijo `Controller`


- Clases de Servicios:
Cada entidad tendrá una interfaz que declarar sus métodos y una clase que lo implementara. el principal objetivo de las clases de servicios es la de implementar la lógica de negocio de la aplicación. En esta clases no se deberá de acceder directamente a los datos o realizar llamadas http, para realizar estas acciones se deberá de utilizar los repositorios.
Ubicación:
	- Interfaces : `Application.Core/Interfaces/Services/{NombreDominio.NombreEntidad}/`

	- Implementación de la interfaz: `Application.Core/Services/{NombreDominio.NombreEntidad}/`

- Factory de repositorios (Abstrac Factory)
Este patrón se implemento bajo la necesidad del cliente de poder acceder a la implementación de un repositorio en tiempo de ejecución. Cada entidad tendrá su método dentro de la factory que retornara la clase que implemente el método deseado.
Ubicación:

	- interfaces : `Application.Core/Interfaces/AbstractFactory/IAbstractFactory`

	- implementación : `Application.Infraestructure/AbstractFactory/AbstractFactory`

- Repositorios
Cada entidad tendrá una interfaz que expondrá los métodos para el acceso a datos y otra clase para su implementación.
Ubicación:
	- interfaces: `Application.Core/Interfaces/Repositories/{NombreDominio.NombreEntidad}/`
	- implementación: ` Application.Infraestructure/Repositories/{NombreDominio.NombreEntidad}/`

	Para el acceso a los repositorios basados en SQL Server se implemento  un repositorio generico con el fin de facilitar el acceso a los metodos basicos sobre EF core `GetByIdTracked, GetByIdAndMap, GetIQueryable, BeginTransaction, CommitTransaction,
	RollbackTransaction, Create, Update,Delete` ademas de realizar la implementacion una clase de extencion para aportar funciones auxiliares de paginado `GetPagedAsNoTracking, GetPaged, GetPagedAndMap`, esta ultima realiza el paginado de una entidad y retorna un value object mapeado por automapper

  

***Manejo de excepciones***  

Las diferentes Apis se configuraron para la utilización de un middelware especifico que será el encargado de el manejo de excepciones, por lo que no resulta necesario la utilización de try/catch en los métodos salvo en excepciones que quieran ser tratadas puntualmente como por ejemplo en el acceso a datos. es posible registrar excepciones personalizadas y asignarles un handler especifico para su manipulación. como salida siempre se retornara un ResultObject con un estado de error http 500

***Log de eventos***

Cada api esta configurada con el paquete "Serilog" para la escritura de logs. estos se almacenan en archivo y en application insights.

***Seguridad***

---
##  Autores

A/S Richard Pias 2024

  ---

##  Changelog  

Todos los cambios realizados se registraran en esta sección. 

El formato esta basado en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), y la nomenclatura para las versiones en [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
  

##  [1.0.0] - 2022-06-24  
##  [2.0.0] - 2024-09-04  

###  Added  
Version actualizada


---
 
 ***Referencias***

Azure Devops - Metodologies

	https://learn.microsoft.com/es-es/azure/devops/boards/get-started/plan-track-work?view=azure-devops&tabs=agile-process

Clean arquitecture

	https://docs.microsoft.com/es-es/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures

	https://github.com/dotnet-architecture/eBooks/raw/main/current/architecting-modern-web-apps-azure/Architecting-Modern-Web-Applications-with-ASP.NET-Core-and-Azure.pdf?WT.mc_id=dotnet-35129-website

Factory pattern

	https://refactoring.guru/es/design-patterns/factory-method

.Net 8

	https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/overview

Serilog

	https://serilog.net/

Swagger

	https://swagger.io/

Redis

	https://redis.io/topics/introduction

	https://docs.microsoft.com/es-es/azure/azure-cache-for-redis/cache-overview

Rest

	https://openwebinars.net/blog/que-es-rest-conoce-su-potencia/

***Seguridad***
 

Auth0

https://auth0.com/universal-login

https://auth0.com/docs/architecture-scenarios/web-app-sso

  
***Azure***

App Service

https://docs.microsoft.com/en-us/azure/app-service/

https://azure.github.io/AppService
  

Slots

https://docs.microsoft.com/en-us/azure/app-service/deploy-staging-slots
  

Continuous Integration / Continuous deployment

https://docs.microsoft.com/en-us/azure/architecture/example-scenario/apps/devops-dotnet-webapp#:~:text=Continuous%20integration%20triggers%20application%20build,deployed%20to%20Azure%20App%20Service.

https://docs.microsoft.com/en-us/sharepoint/dev/spfx/toolchain/implement-ci-cd-with-azure-devops

https://www.azuredevopslabs.com/labs/azuredevops/continuousintegration/

---
