-- Rename stored edge label property key (Phase 7 product rename).
-- Also correct any rows migrated with the mistaken '$ArchiLucid:EdgeLabel' spelling.
UPDATE dbo.GraphSnapshotEdgeProperties
SET PropertyKey = N'$ArchLucid:EdgeLabel'
WHERE PropertyKey IN (N'$ArchiForge:EdgeLabel', N'$ArchiLucid:EdgeLabel');
