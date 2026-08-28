CREATE OR REPLACE FUNCTION public.sp_insert_product(
    p_name VARCHAR,
    p_description TEXT,
    p_price NUMERIC(10,2),
    p_stock INT,
    p_product_category_id INT8,
    p_created_by INT8
)
RETURNS INT8 AS $$
DECLARE
    v_new_product_id INT8;
BEGIN
    INSERT INTO public."Products" (
        "Name",
        "Description",
        "Price",
        "Stock",
        "ProductCategoryId",
        "CreatedAt",
        "CreatedBy"
    ) VALUES (
        p_name,
        p_description,
        p_price,
        p_stock,
        p_product_category_id,
        CURRENT_TIMESTAMP,
        p_created_by
    )
    RETURNING "Id" INTO v_new_product_id;
    
    RETURN v_new_product_id;
END;
$$ LANGUAGE plpgsql;
