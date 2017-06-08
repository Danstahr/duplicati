CREATE TABLE "BlocksetEntry_Temp" (
	"BlocksetID" INTEGER NOT NULL,
	"BlockID" INTEGER NOT NULL,
	"Index" INTEGER NOT NULL,
	"Offset" INTEGER NOT NULL,	
	CONSTRAINT "BlocksetEntry_PK_IdIndex" PRIMARY KEY ("BlocksetID", "Index")
) {#if sqlite_version >= 3.8.2} WITHOUT ROWID {#endif};

/* As this table is a cross table we need fast lookup */
CREATE INDEX "BlocksetEntry_Temp_IndexIdsBackwards" ON "BlocksetEntry_Temp" ("BlockID");

-- The conversion could be done by using SQL as well (query below), but SQLite doesn't support
-- OVER keyword so the query requires cross joining and the entire process takes N^2 instead
-- of N, which is unacceptable for large databases.

-- SELECT
--   A.BlocksetId,
--   A."Index",
--   A.BlockId,
--   CASE WHEN A."Index" = 0 THEN 0 ELSE SUM(C.Size) END AS offset
-- FROM BlocksetEntry A
-- LEFT JOIN BlocksetEntry B
--   ON A.BlocksetId = B.BlocksetId
-- LEFT JOIN Block C
--   ON B.BlockId = C.Id
-- WHERE (A."Index" > B."Index" OR A."Index" = 0)
-- GROUP BY 
--   A.BlocksetId,
--   A."Index",
--   A.BlockId)
--   
-- UPDATE "Version" SET "Version" = 7;  