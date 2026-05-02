# Medycally

Sistema web de gestión de citas médicas para clínicas y consultorios privados.

---

## Características

- **Wizard de citas** — flujo guiado para agendar citas médicas seleccionando especialidad, clínica, médico y horario disponible
- **Gestión de pacientes** — registro de adultos y menores de edad con su representante legal, grupo familiar vinculado
- **Dashboard operativo** — vista diaria de citas por clínica con control de estatus y registro de pacientes
- **Cola de atención médica** — acceso por médico autenticado, historial clínico, diagnóstico y tratamiento
- **Panel de administración** — gestión de especialidades, clínicas, médicos, horarios y usuarios del sistema

---

## Stack

- **Backend:** ASP.NET Core 10.0 MVC (.NET 10)
- **Base de datos:** SQL Server
- **Frontend:** Razor Views · Bootstrap 5 · Font Awesome 6 · JavaScript vanilla
- **Autenticación:** Cookie auth con roles y permisos por módulo

---

## Requisitos

- .NET 10 SDK
- SQL Server (Express o superior)

---

## Instalación

1. Clonar el repositorio
2. Crear la base de datos en SQL Server y ejecutar los scripts de la carpeta `Database/` en el orden indicado por su nombre
3. Configurar la cadena de conexión en `appsettings.Development.json` (no incluido en el repositorio)
4. Ejecutar con `dotnet run`

---

## Licencia

Proyecto privado Carlos Alberto Ceballos. Todos los derechos reservados.
****
