global using Xunit;
global using Xunit.Abstractions;
global using System;
global using System.Threading.Tasks;

// Prefer application DTOs in tests to avoid ambiguity with infrastructure DTOs
global using InventoryItemDto = IMS.Api.Application.Common.DTOs.InventoryItemDto;
global using ReservationDto = IMS.Api.Application.Common.DTOs.ReservationDto;
global using DashboardMetricsDto = IMS.Api.Application.Common.DTOs.DashboardMetricsDto;
global using WarehouseStockDto = IMS.Api.Application.Common.DTOs.WarehouseStockDto;
global using ProductDto = IMS.Api.Application.Common.DTOs.ProductDto;
global using StockMovementDto = IMS.Api.Application.Common.DTOs.StockMovementDto;
global using LowStockVariantDto = IMS.Api.Application.Common.DTOs.LowStockVariantDto;
global using AuditLogDto = IMS.Api.Application.Common.DTOs.AuditLogDto;
