CREATE OR REPLACE FUNCTION public.sp_update_product(
    p_id INT8,
    p_name VARCHAR,
    p_description TEXT,
    p_price NUMERIC(10,2),
    p_stock INT,
    p_product_category_id INT8,
    p_updated_by INT8
)
RETURNS VOID AS $$
BEGIN
    UPDATE public."Products"
    SET
        "Name" = p_name,
        "Description" = p_description,
        "Price" = p_price,
        "Stock" = p_stock,
        "ProductCategoryId" = p_product_category_id,
        "UpdatedAt" = CURRENT_TIMESTAMP,
        "UpdatedBy" = p_updated_by
    WHERE "Id" = p_id;
END;
$$ LANGUAGE plpgsql;
