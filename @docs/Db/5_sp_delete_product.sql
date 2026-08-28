CREATE OR REPLACE FUNCTION public.sp_delete_product(
    p_id INT8,
    p_deleted_by INT8
)
RETURNS VOID AS $$
BEGIN
    UPDATE public."Products"
    SET
        "DeletedAt" = CURRENT_TIMESTAMP,
        "DeletedBy" = p_deleted_by
    WHERE "Id" = p_id;
END;
$$ LANGUAGE plpgsql;
