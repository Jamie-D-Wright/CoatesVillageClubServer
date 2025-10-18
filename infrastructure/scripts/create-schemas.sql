-- =============================================
-- Village Club Management Database Schemas
-- Version: 1.0.0
-- Date: 2025-10-18
-- =============================================

-- This script creates the five database schemas for the microservices architecture
-- Each schema is owned by a single microservice to maintain service boundaries

USE [VillageClubDB];
GO

-- =============================================
-- Create Schemas
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Membership')
BEGIN
    EXEC('CREATE SCHEMA [Membership]');
    PRINT 'Schema [Membership] created successfully';
END
ELSE
BEGIN
    PRINT 'Schema [Membership] already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Events')
BEGIN
    EXEC('CREATE SCHEMA [Events]');
    PRINT 'Schema [Events] created successfully';
END
ELSE
BEGIN
    PRINT 'Schema [Events] already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Scheduling')
BEGIN
    EXEC('CREATE SCHEMA [Scheduling]');
    PRINT 'Schema [Scheduling] created successfully';
END
ELSE
BEGIN
    PRINT 'Schema [Scheduling] already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Bar')
BEGIN
    EXEC('CREATE SCHEMA [Bar]');
    PRINT 'Schema [Bar] created successfully';
END
ELSE
BEGIN
    PRINT 'Schema [Bar] already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Finance')
BEGIN
    EXEC('CREATE SCHEMA [Finance]');
    PRINT 'Schema [Finance] created successfully';
END
ELSE
BEGIN
    PRINT 'Schema [Finance] already exists';
END
GO

PRINT '=============================================';
PRINT 'All schemas created successfully!';
PRINT '=============================================';
PRINT '';
PRINT 'Schemas:';
PRINT '  - Membership (Users, Authentication, Roles)';
PRINT '  - Events (Event Calendar, Event Management)';
PRINT '  - Scheduling (Shifts, Volunteer Assignments)';
PRINT '  - Bar (Stock Alerts, Inventory)';
PRINT '  - Finance (Expenses, Receipts, Reimbursements)';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Run EF Core migrations for each service to create tables';
PRINT '2. Configure service-specific database users and permissions';
PRINT '3. Enable auditing and monitoring';
GO
