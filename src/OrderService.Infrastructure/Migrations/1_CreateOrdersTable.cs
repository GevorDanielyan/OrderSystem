using FluentMigrator;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Migrations;

[Migration(1)]
public class _1_CreateOrdersTable : Migration
{
    public override void Down()
    {
        Delete.Table("orders");
    }

    public override void Up()
    {
        Create.Table("orders")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("customer_name").AsString(255).NotNullable().Indexed("idx_orders_customer_name")
            .WithColumn("amount").AsDecimal().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("status").AsInt32().NotNullable().Indexed("idx_orders_status").WithDefaultValue((int)OrderStatus.Pending);
    }
}