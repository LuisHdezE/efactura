CREATE OR REPLACE FUNCTION public.fn_get_products_paginated(
    p_page INT,
    p_rows_per_page INT
)
RETURNS TABLE(
    id INT8,
    name VARCHAR,
    description TEXT,
    price NUMERIC(10,2),
    stock INT4,
    productcategoryid INT8,
    createdat TIMESTAMP(6),
    createdby INT8,
    updatedat TIMESTAMP(6),
    updatedby INT8,
    deletedat TIMESTAMP(6),
    deletedby INT8,
    totalrecords INT
) AS $$
BEGIN
    RETURN QUERY
    WITH total AS (
        SELECT COUNT(*) AS totalrecords
        FROM public."Products"
        WHERE "DeletedAt" IS NULL
    ),
    paginated_products AS (
        SELECT *
        FROM public."Products"
        WHERE "DeletedAt" IS NULL
        ORDER BY "Id"
        OFFSET (p_page - 1) * p_rows_per_page
        LIMIT p_rows_per_page
    )
    SELECT
        p."Id" AS id,
        p."Name" AS name,
        p."Description" AS description,
        p."Price" AS price,
        p."Stock" AS stock,
        p."ProductCategoryId" AS productcategoryid,
        p."CreatedAt" AS createdat,
        p."CreatedBy" AS createdby,
        p."UpdatedAt" AS updatedat,
        p."UpdatedBy" AS updatedby,
        p."DeletedAt" AS deletedat,
        p."DeletedBy" AS deletedby,
        t.totalrecords
    FROM paginated_products p CROSS JOIN total t;
END;
$$ LANGUAGE plpgsql;
