#ifndef LAYER_H
#define LAYER_H
#include "pugixml.hpp"
#include "Frame.h"
#include <vector>
#include <array>
#include <memory>
class Layer {
	friend class Timeline;
private:
	pugi::xml_node root;
	void loadFrames(pugi::xml_node& layerNode) noexcept;
	bool insertKeyframe(unsigned int frameIndex, bool isBlank) noexcept;
	void removeKeyframe(unsigned int keyframeIndex) noexcept;
	std::vector<std::unique_ptr<Frame>> frames;
	std::string color;
	std::string layerType;
	std::optional<unsigned int> parentLayerIndex;
	bool locked, current, selected;
	std::string name;
public:
	Layer(pugi::xml_node& layerNode) noexcept;
	Layer(const Layer& layer) noexcept;
	~Layer() noexcept;
	bool insertKeyframe(unsigned int frameIndex) noexcept;
	bool insertBlankKeyframe(unsigned int frameIndex) noexcept;
	bool clearKeyFrame(unsigned int frameIndex) noexcept;
	Frame* getKeyFrame(unsigned int index) const noexcept;
	unsigned int getKeyframeIndex(unsigned int frameIndex) const noexcept;
	Frame* getFrame(unsigned int frameIndex) const noexcept;
	const std::string& getColor() const noexcept;
	void setColor(const std::string& color) noexcept;
	const std::string& getLayerType() const noexcept;
	void setLayerType(const std::string& layerType) noexcept(false);
	bool isLocked() const noexcept;
	void setLocked(bool locked) noexcept;
	bool isCurrent() const noexcept;
	void setCurrent(bool current) noexcept;
	bool isSelected() const noexcept;
	void setSelected(bool selected) noexcept;
	const std::string& getName() const noexcept;
	void setName(const std::string& name) noexcept;
	std::optional<unsigned int> getParentLayerIndex() const noexcept;
	void setParentLayerIndex(std::optional<unsigned int> parentLayer) noexcept;
	unsigned int getFrameCount() const noexcept;
	pugi::xml_node& getRoot() noexcept;
	const pugi::xml_node& getRoot() const noexcept;
};
constexpr std::array<std::string_view, 6> ACCEPTABLE_LAYER_TYPES = { "normal", "guide", "guided", "mask", "masked", "folder" };
#endif // LAYER_H