# Gemipedia
Gemini frontend to Wikipedia

gemini://gemi.dev/cgi-bin/wp.cgi/

## Features
* Supports fuzzy matching for finding articles
* Groups all the links to additional articles by section, and separate "References" pages for each section
* Gallery View, which pulls all media like images and video out into a separate view
* Supports tables, including cells that span multiple rows or columns, by converting them to ASCII art tables inside of preformatted sections
* Supports math formulas, by fetches the original SVG images and converting them on the fly to PNG
* Caching calls to Wikipedia to speed up viewing sub sections or page refreshing
* PDF export for offline reading
