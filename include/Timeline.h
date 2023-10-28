#ifndef TIMELINE_H
#define TIMELINE_H
#include "pugixml.hpp"
#include "Layer.h"
#include <vector>
#include <memory>
class Timeline {
private:
	std::vector<std::unique_ptr<Layer>> layers;
	pugi::xml_node root;
	std::string name;
	void loadLayers(pugi::xml_node& timelineNode) noexcept;
public:
	Timeline(pugi::xml_node& timelineNode) noexcept;
	~Timeline() noexcept;
	unsigned int getFrameCount() const noexcept;
	unsigned int getLayerCount() const noexcept;
	unsigned int addNewLayer(const std::string& name = "New Layer", const std::string& layerType = "normal") noexcept;
	const std::vector<unsigned int> findLayerIndex(const std::string& name) const noexcept;
	void deleteLayer(unsigned int index) noexcept;
	Layer* getLayer(unsigned int index) const noexcept;
	const std::string& getName() const noexcept;
	void setName(const std::string& name) noexcept;
	pugi::xml_node& getRoot() noexcept;
	const pugi::xml_node& getRoot() const noexcept;
};
#endif // TIMELINE_H