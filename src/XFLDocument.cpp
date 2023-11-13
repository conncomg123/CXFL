#include "../include/XFLDocument.h"
#include <stdexcept>
#include <sstream>
void XFLDocument::loadTimelines(pugi::xml_node& root) {
	auto timelines = root.child("timelines").children("DOMTimeline");
	for (auto iter = timelines.begin(); iter != timelines.end(); ++iter) {
		this->timelines.push_back(std::make_unique<Timeline>(*iter));
	}
}
void XFLDocument::load_xfl(const std::string& filename) {
	// if filename ends with xfl, then we're good
	// otherwise, throw an exception
	this->filename = filename;
	xflTree = std::make_unique<pugi::xml_document>();
	auto result = xflTree->load_file(filename.c_str());
	if (!result) {
		throw std::runtime_error("Failed to load XFL file: " + std::string(result.description()));
	}
	this->root = xflTree->document_element();
}
void XFLDocument::load_fla(const std::string& filename) {
    // Open the zip archive with minizip
    void* zip_reader = mz_zip_reader_create();
    if (mz_zip_reader_open_file_in_memory(zip_reader, filename.c_str()) != MZ_OK) {
		mz_zip_reader_delete(&zip_reader);
        throw std::runtime_error("Failed to open file: " + filename);
    }
    if(mz_zip_reader_locate_entry(zip_reader, "DOMDocument.xml", 0) != MZ_OK) {
		mz_zip_reader_delete(&zip_reader);
		mz_zip_reader_close(zip_reader);
        throw std::runtime_error("Failed to locate DOMDocument.xml in file: " + filename);
    }
	int32_t buf_size = (int32_t)mz_zip_reader_entry_save_buffer_length(zip_reader);
	char *buf = (char *)malloc(buf_size);
	int32_t err = mz_zip_reader_entry_save_buffer(zip_reader, buf, buf_size);
	if (err == MZ_OK) {
		this->xflTree = std::make_unique<pugi::xml_document>();
		pugi::xml_parse_result result = this->xflTree->load_buffer(buf, buf_size);
		if(!result) {
			free(buf);
			mz_zip_reader_close(zip_reader);
			mz_zip_reader_delete(&zip_reader);
			throw std::runtime_error("Failed to parse DOMDocument.xml in file: " + filename);
		}
		this->root = this->xflTree->document_element();
	}
	else {
		free(buf);
		mz_zip_reader_close(zip_reader);
		mz_zip_reader_delete(&zip_reader);
		throw std::runtime_error("Failed to read DOMDocument.xml in file: " + filename);
	}
	mz_zip_reader_close(zip_reader);
	mz_zip_reader_delete(&zip_reader);
	free(buf);
}
XFLDocument::XFLDocument(const std::string& filename) {
	// if filename ends with xfl, then we're good
	// otherwise, throw an exception
	if (filename.substr(filename.length() - 4) == ".xml") {
		load_xfl(filename);
	}
	else if (filename.substr(filename.length() - 4) == ".fla") {
		load_fla(filename);
	}
	else {
		throw std::runtime_error("Invalid file extension");
	}
	loadTimelines(this->root);
}
XFLDocument::~XFLDocument() {

}

void XFLDocument::save_xfl(const std::string& filename) {
	this->xflTree->save_file(filename.c_str());
}

int32_t minizip_erase(const char *src_path, const char *target_path, int32_t arg_count, const char **args) {
    mz_zip_file *file_info = NULL;
    const char *filename_in_zip = NULL;
    const char *target_path_ptr = target_path;
    void *reader = NULL;
    void *writer = NULL;
    int32_t skip = 0;
    int32_t err = MZ_OK;
    int32_t i = 0;
    uint8_t zip_cd = 0;
    char bak_path[256];
    char tmp_path[256];

    if (target_path == NULL) {
        /* Construct temporary zip name */
        strncpy(tmp_path, src_path, sizeof(tmp_path) - 1);
        tmp_path[sizeof(tmp_path) - 1] = 0;
        strncat(tmp_path, ".tmp.zip", sizeof(tmp_path) - strlen(tmp_path) - 1);
        target_path_ptr = tmp_path;
    }

    reader = mz_zip_reader_create();
    writer = mz_zip_writer_create();

    /* Open original archive we want to erase an entry in */
    err = mz_zip_reader_open_file(reader, src_path);
    if (err != MZ_OK) {
        printf("Error %" PRId32 " opening archive for reading %s\n", err, src_path);
        mz_zip_reader_delete(&reader);
        return err;
    }

    /* Open temporary archive */
    err = mz_zip_writer_open_file(writer, target_path_ptr, 0, 0);
    if (err != MZ_OK) {
        printf("Error %" PRId32 " opening archive for writing %s\n", err, target_path_ptr);
        mz_zip_reader_delete(&reader);
        mz_zip_writer_delete(&writer);
        return err;
    }

    err = mz_zip_reader_goto_first_entry(reader);

    if (err != MZ_OK && err != MZ_END_OF_LIST)
        printf("Error %" PRId32 " going to first entry in archive\n", err);

    while (err == MZ_OK) {
        err = mz_zip_reader_entry_get_info(reader, &file_info);
        if (err != MZ_OK) {
            printf("Error %" PRId32 " getting info from archive\n", err);
            break;
        }

        /* Copy all entries from original archive to temporary archive
           except the ones we don't want */
        for (i = 0, skip = 0; i < arg_count; i += 1) {
            filename_in_zip = args[i];

            if (mz_path_compare_wc(file_info->filename, filename_in_zip, 1) == MZ_OK)
                skip = 1;
        }

        if (skip) {
            // printf("Skipping %s\n", file_info->filename);
        } else {
            // printf("Copying %s\n", file_info->filename);
            err = mz_zip_writer_copy_from_reader(writer, reader);
        }

        if (err != MZ_OK) {
            printf("Error %" PRId32 " copying entry into new zip\n", err);
            break;
        }

        err = mz_zip_reader_goto_next_entry(reader);

        if (err != MZ_OK && err != MZ_END_OF_LIST)
            printf("Error %" PRId32 " going to next entry in archive\n", err);
    }

    mz_zip_reader_get_zip_cd(reader, &zip_cd);
    mz_zip_writer_set_zip_cd(writer, zip_cd);

    mz_zip_reader_close(reader);
    mz_zip_reader_delete(&reader);

    mz_zip_writer_close(writer);
    mz_zip_writer_delete(&writer);

    if (err == MZ_END_OF_LIST) {
        if (target_path == NULL) {
            /* Swap original archive with temporary archive, backup old archive if possible */
            strncpy(bak_path, src_path, sizeof(bak_path) - 1);
            bak_path[sizeof(bak_path) - 1] = 0;
            strncat(bak_path, ".bak", sizeof(bak_path) - strlen(bak_path) - 1);

            if (mz_os_file_exists(bak_path) == MZ_OK)
                mz_os_unlink(bak_path);

            if (mz_os_rename(src_path, bak_path) != MZ_OK)
                printf("Error backing up archive before replacing %s\n", bak_path);

            if (mz_os_rename(tmp_path, src_path) != MZ_OK)
                printf("Error replacing archive with temp %s\n", tmp_path);
        }

        return MZ_OK;
    }

    return err;
}

void XFLDocument::save_fla(const std::string& filename) {
	const char *args[1] = { "DOMDocument.xml" };
	minizip_erase(filename.c_str(), NULL, 1, args);
	void* zip_writer = mz_zip_writer_create();
	if (mz_zip_writer_open_file(zip_writer, filename.c_str(), 0, 1) != MZ_OK) {
		mz_zip_writer_delete(&zip_writer);
		throw std::runtime_error("Failed to open file: " + filename);
	}
	std::stringstream ss;
	this->xflTree->save(ss);
	mz_zip_file file_info = {0};
	file_info.filename = "DOMDocument.xml";
	file_info.compression_method = MZ_COMPRESS_METHOD_DEFLATE;
	file_info.uncompressed_size = ss.str().length();
	file_info.modified_date = time(NULL);
	file_info.flag = 2050;
	file_info.version_madeby = 20;
	file_info.external_fa = 0;
	file_info.internal_fa = 0;
	file_info.filename_size = strlen(file_info.filename);
	mz_zip_writer_set_compress_method(zip_writer, MZ_COMPRESS_METHOD_DEFLATE);
	mz_zip_writer_add_buffer(zip_writer, (void*) ss.str().c_str(), ss.str().length(), &file_info);
	mz_zip_writer_close(zip_writer);
	mz_zip_writer_delete(&zip_writer);
}
void XFLDocument::write(const std::string& filename) {
	if (filename.substr(filename.length() - 4) == ".xml") {
		save_xfl(filename);
	}
	else if (filename.substr(filename.length() - 4) == ".fla") {
		save_fla(filename);
	}
	else {
		throw std::runtime_error("Invalid file extension");
	}
}
unsigned int XFLDocument::duplicateTimeline(unsigned int index) {
    auto dupedTimeline = std::make_unique<Timeline>(*this->getTimeline(index));
    dupedTimeline->setName(dupedTimeline->getName() + "_copy");
    this->timelines.emplace(this->timelines.begin() + index, std::move(dupedTimeline));
    return index + 1;
}
Timeline* XFLDocument::getTimeline(unsigned int index) {
	return timelines[index].get();
}
pugi::xml_node& XFLDocument::getRoot() {
	return this->root;
}